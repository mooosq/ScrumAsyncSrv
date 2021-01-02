using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Security.Cryptography;

namespace ServAsync
{
    public class Server : IDisposable
    {
        string ipAddr;
        int port;
        TextBox log = null;
        Database db;
        Dictionary<string, int> loggedIn = new Dictionary<string, int>();
        private byte[] buffer = new byte[1024];
        private List<Socket> clientSockets = new List<Socket>();
        private Socket servSocekt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private byte[] enterLogin = Encoding.ASCII.GetBytes("Please enter login: ");
        private byte[] enterPassword = Encoding.ASCII.GetBytes("Please enter password: ");
        private byte[] welcomeMessage = Encoding.ASCII.GetBytes("Welcome on the server!\r\nType 'login' to log in or 'register' to create account.\r\nYour choice: ");
        private byte[] invalidRequest = Encoding.ASCII.GetBytes("Invalid request, please try again: ");
        private byte[] createMessage = Encoding.ASCII.GetBytes("Account created successfully!\r\nType your choice again: ");
        private byte[] wrongCredentials = Encoding.ASCII.GetBytes("Wrong credentials!\r\nType your choice again: ");
        private byte[] again = Encoding.ASCII.GetBytes("\r\nType 'login' or 'register': ");
        private byte[] logInfo = Encoding.ASCII.GetBytes("Logged succesfully!\r\nWant to play a game? Type 'yes' or 'no' to log out and exit: ");
        private byte[] logOutInfo = Encoding.ASCII.GetBytes("User with this login is still logged in! Returning to the main...\n");
        private byte[] backToMain = Encoding.ASCII.GetBytes("Exiting to main... Write 'login' or 'register' and retype credentials: ");
        private byte[] userAlreadyExistMessage = Encoding.ASCII.GetBytes("User already exist. Type 'login' or 'register': ");


        public void Dispose()
        {
            servSocekt.Dispose();
        }

        public Server(ref TextBox txtBox, string ipAddr, string port)
        {
            this.ipAddr = ipAddr;
            _ = int.TryParse(port, out this.port);
            log = txtBox;
            db = new Database(log);
            db.setupDatabase();
        } 

        /// <summary>
        /// Glowna funkcja uruchamiajaca serwer
        /// </summary>
        public void SetupServer()
        {
            servSocekt.Bind(new IPEndPoint(IPAddress.Parse(ipAddr), port));
            log.Dispatcher.Invoke(delegate {
                log.Text += $"Setting up the server...\nIP address: {ipAddr}\nPort: {port}\n";
            });
            servSocekt.Listen(10);
            servSocekt.BeginAccept(new AsyncCallback(AcceptCallback), null);
            log.Dispatcher.Invoke(delegate
            {
                log.Text += "Configuration complete!\n";
            });
        }

        public List<Socket> getClients()
        {
            return clientSockets;
        }

        /// <summary>
        /// Funkcja zatrzymujaca dzialanie serwera - zamyka gniazdo
        /// </summary>
        public void StopServer()
        {
            foreach (Socket sock in clientSockets)
                sock.Close();

            LingerOption lo = new LingerOption(false, 0);
            servSocekt.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lo);
            log.Dispatcher.Invoke(delegate
            {
                log.Text += $"[{DateTime.Now}] All clients disconnected. Server stopped\n";
            });
        }

        /// <summary>
        /// Callback akceptujacy polaczenie klienta
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket;
            try
            {
                 socket = servSocekt.EndAccept(ar);
            } catch (ObjectDisposedException ex)
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Work ended properly\n";
                });
                return;
            }
            clientSockets.Add(socket);
            log.Dispatcher.Invoke(delegate
            {
                log.Text += $"[{DateTime.Now}] Client connected\n";
            });
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);
            socket.BeginSend(welcomeMessage, 0, welcomeMessage.Length, SocketFlags.None, SendCallback, socket);
            servSocekt.BeginAccept(AcceptCallback, null);
        }

        /// <summary>
        /// Callback wyboru opcji logowania lub rejestracji
        /// </summary>
        /// <param name="ar"></param>
        private void ChoiceCallback(IAsyncResult ar)
        {

            Socket socket = (Socket)ar.AsyncState;
            try
            {
                int isLogged;
                int received = socket.EndReceive(ar);
                byte[] tempBuffer = new byte[received];
                byte[] login = new byte[1024];
                Array.Copy(buffer, tempBuffer, received);

                string request = null, text = Encoding.ASCII.GetString(tempBuffer);
                if (text != "\r\n")
                {
                    request = text.ToLower().Trim('\r');
                    request = request.Trim('\n');

                    if (text != "")
                        log.Dispatcher.Invoke(delegate
                        {
                            log.Text += $"[{DateTime.Now}] Received request: {request}\n";
                        });

                    if (request == "exit")
                    {
                        socket.Close();
                        clientSockets.Remove(socket);
                    }
                    else if (request == "login" || request == "gin")
                    {
                        socket.BeginSend(enterLogin, 0, enterLogin.Length, SocketFlags.None, SendCallback, socket);
                        login = Receive(socket);
                        Login(socket, login);
                    }
                    else if (request == "register" || request == "gister")
                        Register(socket);
                    else
                        socket.BeginSend(invalidRequest, 0, invalidRequest.Length, SocketFlags.None, SendCallback, socket);
                }

                if (request == "login" || request == "gin" && loggedIn.ContainsKey(Encoding.ASCII.GetString(login).Trim('\0')))
                {
                    if (loggedIn.TryGetValue(Encoding.ASCII.GetString(login).Trim('\0'), out isLogged) && isLogged == 1)
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, GameCallback, socket);
                }
                else
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);

            }
            catch
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Client disconnetcted\n";
                });
                socket.Close();
                clientSockets.Remove(socket);
            }
        }

        /// <summary>
        /// Funkcja odpowiadajaca za logowanie klienta po polaczeniu sie z serwerem
        /// </summary>
        /// <param name="socket">Aktualne gniazdo, z ktorym skojarzony jest klient</param>
        /// <param name="login">Login uzytkownika</param>
        private void Login(Socket socket, byte[] login)
        {
            try
            {
                socket.BeginSend(enterPassword, 0, enterPassword.Length, SocketFlags.None, SendCallback, socket);
                String pass = Encoding.ASCII.GetString(Receive(socket)).Trim('\0');
                String userLogin = Encoding.ASCII.GetString(login).Trim('\0');
                
                if (pass == db.userPassword(userLogin))
                {
                    try
                    {
                        loggedIn.Add(userLogin, 1);
                        socket.BeginSend(logInfo, 0, logInfo.Length, SocketFlags.None, SendCallback, socket);
                        log.Dispatcher.Invoke(delegate
                        {
                            log.Text += $"[{DateTime.Now}] User '{userLogin}' logged in\n";
                        });
                    }
                    catch
                    {
                        log.Dispatcher.Invoke(delegate
                        {
                            log.Text += $"[{DateTime.Now}] Client '{userLogin}' is still logged in, dissconnecting the client...\n";
                        });
                        socket.Close();
                        clientSockets.Remove(socket);
                    }
                }
                else
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);
                    socket.BeginSend(wrongCredentials, 0, wrongCredentials.Length, SocketFlags.None, SendCallback, socket);
                }
            }
            catch
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Client disconnetcted\n";
                });
                socket.Close();
                clientSockets.Remove(socket);
            }
        }

        /// <summary>
        /// Glowny callback gry, to tutaj obslugiwana jest rozgrywka
        /// </summary>
        /// <param name="ar"></param>
        private void GameCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;

            try
            {
                byte[] answer = Receive(socket);
                string answerOut = Encoding.ASCII.GetString(answer).Trim('\0').ToLower();
                int counter, index;

                if (answerOut != "\r\ns")
                {
                    socket.BeginSend(backToMain, 0, backToMain.Length, SocketFlags.None, SendCallback, socket);
                    string userKey = FindUser(clientSockets.IndexOf(socket));
                    log.Dispatcher.Invoke(delegate
                    {
                        log.Text += $"[{DateTime.Now}] User '{userKey}' logged out\n";
                    });
                }
                else
                {
                    bool guess = false;
                    byte[] type = new byte[1024];

                    byte[] randomBytes = new byte[4];
                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                    rng.GetBytes(randomBytes);

                    int userInput, randomInteger = (int)(BitConverter.ToUInt32(randomBytes, 0) % 101);

                    byte[] randomInfo = Encoding.ASCII.GetBytes("Just get randomly selected number for you, try guess it!\r\nYour type: ");
                    byte[] tooBig = Encoding.ASCII.GetBytes("Too big number! Enter your type again: ");
                    byte[] tooSmall = Encoding.ASCII.GetBytes("Too small number! Enter your type again: ");
                    socket.BeginSend(randomInfo, 0, randomInfo.Length, SocketFlags.None, SendCallback, socket);
                    while (!guess)
                    {
                        socket.Receive(type);
                        if (Encoding.ASCII.GetString(type).Trim('\0') != "\r\n")
                        {
                            int.TryParse(Encoding.ASCII.GetString(type).Trim('\0').Trim('\r').Trim('\n'), out userInput);
                            if (userInput > randomInteger)
                            {
                                socket.BeginSend(tooBig, 0, tooBig.Length, SocketFlags.None, SendCallback, socket);
                            }
                            else if (userInput < randomInteger)
                            {
                                socket.BeginSend(tooSmall, 0, tooSmall.Length, SocketFlags.None, SendCallback, socket);
                            }
                            else if (userInput == randomInteger)
                            {
                                byte[] success = Encoding.ASCII.GetBytes($"You got it! The number is {randomInteger}. Thanks for playing!");
                                socket.BeginSend(success, 0, success.Length, SocketFlags.None, SendCallback, socket);
                                guess = true;
                                string userKey = FindUser(clientSockets.IndexOf(socket));
                                log.Dispatcher.Invoke(delegate
                                {
                                    log.Text += $"[{DateTime.Now}] User '{userKey}' guessed the number. Logging out '{userKey}'...\n";
                                });
                                socket.BeginSend(again, 0, again.Length, SocketFlags.None, SendCallback, socket);
                            }
                        }
                    }
                }

                counter = 0;
                index = clientSockets.IndexOf(socket);
                foreach (KeyValuePair<string, int> user in loggedIn)
                {
                    if (counter == index)
                    {
                        loggedIn.Remove(user.Key);
                        break;
                    }
                    counter++;
                }

                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);
            }
            catch
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Client disconnetcted\n";
                });
                socket.Close();
                clientSockets.Remove(socket);
            }
        }

        /// <summary>
        /// Funkcja odpowiadajaca za obsluge rejestracji uzytkownika
        /// </summary>
        /// <param name="socket">Gniazdko skojarzone z danym klientem</param>
        private void Register(Socket socket)
        {
            try
            {
                socket.BeginSend(enterLogin, 0, enterLogin.Length, SocketFlags.None, SendCallback, socket);
                String login = Encoding.ASCII.GetString(Receive(socket)).Trim('\0');
                socket.BeginSend(enterPassword, 0, enterPassword.Length, SocketFlags.None, SendCallback, socket);
                String pass = Encoding.ASCII.GetString(Receive(socket)).Trim('\0');
                //userData.Add(Encoding.ASCII.GetString(login).Trim('\0'), Encoding.ASCII.GetString(pass).Trim('\0'));

                if(db.addUser(login, pass))
                {
                    log.Dispatcher.Invoke(delegate
                    {
                        log.Text += $"[{DateTime.Now}] Client created account: {login}\n";
                    });
                    socket.BeginSend(createMessage, 0, createMessage.Length, SocketFlags.None, SendCallback, socket);
                }
                else
                {
                    //socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);
                    socket.BeginSend(userAlreadyExistMessage, 0, userAlreadyExistMessage.Length, SocketFlags.None, SendCallback, socket);
                }
            }
            catch
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Client disconnetcted\n";
                });
                socket.Close();
                clientSockets.Remove(socket);
            }
        }

        /// <summary>
        /// Funkcja odbierajaca dane od klienta
        /// </summary>
        /// <param name="socket">Gniazdo skojarzone z danym klientem</param>
        /// <returns></returns>
        private byte[] Receive(Socket socket)
        {
            byte[] bytes = new byte[1024];

            try
            {
                string data = null;
                int counter = 0;
                while (true)
                {
                    int numByte = socket.Receive(bytes);

                    data += Encoding.ASCII.GetString(bytes, 0, numByte);

                    if (data.IndexOf("\r\n") > -1 && counter > 0)
                        break;
                    counter++;
                }
            }
            catch
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Client disconnetcted\n";
                });
                socket.Close();
                clientSockets.Remove(socket);
            }

            return bytes;
        }

        /// <summary>
        /// Funkcja zwracajaca nazwe uzytkownika po numerze gniazda do ktorego podlaczyl sie dany uzytkownik
        /// </summary>
        /// <param name="clientSocketIndex">Klucz danego uzytkownika w slowniku</param>
        /// <returns></returns>
        private string FindUser(int clientSocketIndex)
        {
            string userKey = null;
            int counter = 0;
            foreach (KeyValuePair<string, int> user in loggedIn)
            {
                if (counter == clientSocketIndex)
                {
                    userKey = user.Key;
                    break;
                }
                counter++;
            }
            return userKey;
        }

        /// <summary>
        /// Callback odpowiadajacy za wyslanie danych z serwera do klienta
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;

            try
            {
                socket.EndSend(ar);
            }
            catch
            {
                log.Dispatcher.Invoke(delegate
                {
                    log.Text += $"[{DateTime.Now}] Client disconnetcted\n";
                });
                socket.Close();
                clientSockets.Remove(socket);
            }
        }
    }
}
