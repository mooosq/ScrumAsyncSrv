using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServAsync
{
    class Server
    {
        Dictionary<string, int> loggedIn = new Dictionary<string, int>();
        Dictionary<string, string> userData = new Dictionary<string, string>();
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
        private byte[] logOutInfo = Encoding.ASCII.GetBytes("User with this login is still logged in! Logging out...");
        private byte[] backToMain = Encoding.ASCII.GetBytes("Exiting to main... Write 'login' or 'register' and retype credentials: ");

        /// <summary>
        /// Glowna funkcja uruchamiajaca serwer
        /// </summary>
        public void SetupServer()
        {
            userData.Add("admin","admin");
            Console.Title = "SERVER";
            Console.WriteLine("Setting up the server...");
            servSocekt.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22111));
            servSocekt.Listen(10);
            servSocekt.BeginAccept(new AsyncCallback(AcceptCallback), null);
            Console.WriteLine("Configuration complete!");
        }

        /// <summary>
        /// Callback akceptujacy polaczenie klienta
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = servSocekt.EndAccept(ar);
            clientSockets.Add(socket);
            Console.WriteLine($"[{DateTime.Now}] Client connected");
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
                        Console.WriteLine($"[{DateTime.Now}] Received request: {request}");

                    if (request == "exit")
                    {
                        socket.Close();
                        clientSockets.Remove(socket);
                    }
                    else if (request == "login")
                    {
                        socket.BeginSend(enterLogin, 0, enterLogin.Length, SocketFlags.None, SendCallback, socket);
                        login = Receive(socket);
                        Login(socket, login);
                    }
                    else if (request == "register")
                        Register(socket);
                    else
                        socket.BeginSend(invalidRequest, 0, invalidRequest.Length, SocketFlags.None, SendCallback, socket);
                }

                if (request == "login" && loggedIn.ContainsKey(Encoding.ASCII.GetString(login).Trim('\0')))
                {
                    if (loggedIn.TryGetValue(Encoding.ASCII.GetString(login).Trim('\0'), out isLogged) && isLogged == 1)
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, GameCallback, socket);
                }
                else
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);

            }
            catch
            {
                Console.WriteLine($"[{DateTime.Now}] Client disconnetcted");
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
                byte[] pass = Receive(socket);
                string passOut;
                if (userData.TryGetValue(Encoding.ASCII.GetString(login).Trim('\0'), out passOut) && passOut == Encoding.ASCII.GetString(pass).Trim('\0'))
                {
                    try
                    {
                        loggedIn.Add(Encoding.ASCII.GetString(login).Trim('\0'), 1);
                        socket.BeginSend(logInfo, 0, logInfo.Length, SocketFlags.None, SendCallback, socket);
                        Console.WriteLine($"[{DateTime.Now}] User '{Encoding.ASCII.GetString(login).Trim('\0')}' logged in");
                    }
                    catch
                    {
                        Console.WriteLine($"[{DateTime.Now}] Client '{Encoding.ASCII.GetString(login).Trim('\0')}' is still logged in, dissconnecting the client...");
                        socket.Close();
                        clientSockets.Remove(socket);
                    }
                }
                else
                {
                    socket.BeginSend(wrongCredentials, 0, wrongCredentials.Length, SocketFlags.None, SendCallback, socket);
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ChoiceCallback, socket);
                }
            }
            catch
            {
                Console.WriteLine($"[{DateTime.Now}] Client disconnetcted");
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
                }
                else
                {
                    bool guess = false;
                    byte[] type = new byte[1024];
                    Random random = new Random();
                    int userInput, randomInteger = random.Next() % 101;
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
                                string userKey = null;
                                counter = 0;
                                index = clientSockets.IndexOf(socket);
                                foreach (KeyValuePair<string, int> user in loggedIn)
                                {
                                    if (counter == index)
                                    {
                                        userKey = user.Key;
                                        break;
                                    }
                                    counter++;
                                }
                                Console.WriteLine($"[{DateTime.Now}] User {userKey} guessed the number. Logging out {userKey}...");
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
                Console.WriteLine($"[{DateTime.Now}] Client disconnetcted");
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
                byte[] login = Receive(socket);
                socket.BeginSend(enterPassword, 0, enterPassword.Length, SocketFlags.None, SendCallback, socket);
                byte[] pass = Receive(socket);
                userData.Add(Encoding.ASCII.GetString(login).Trim('\0'), Encoding.ASCII.GetString(pass).Trim('\0'));
                Console.WriteLine($"[{DateTime.Now}] Client created account: {Encoding.ASCII.GetString(login).Trim('\0')}");
                socket.BeginSend(createMessage, 0, createMessage.Length, SocketFlags.None, SendCallback, socket);
            }
            catch
            {
                Console.WriteLine($"[{DateTime.Now}] Client disconnetcted");
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
                Console.WriteLine($"[{DateTime.Now}] Client disconnetcted");
                socket.Close();
                clientSockets.Remove(socket);
            }

            return bytes;
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
                Console.WriteLine($"[{DateTime.Now}] Client disconnetcted");
                socket.Close();
                clientSockets.Remove(socket);
            }
        }
    }
}
