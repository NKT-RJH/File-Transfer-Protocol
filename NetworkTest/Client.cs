using System.Net.Sockets;
using System.Numerics;
using System.Text;

#pragma warning disable CS8600 // 가능한 null 참조 인수입니다.
#pragma warning disable CS8604 // 가능한 null 참조 인수입니다.
#pragma warning disable CS8602 // null 가능 참조에 대한 역참조입니다.
#pragma warning disable CS8625 // Null 리터럴을 null을 허용하지 않는 참조 형식으로 변환할 수 없습니다.

namespace NetworkClient
{
	internal class Client
	{
		// 종료 문자열 상수
		private readonly string breakCommand = "exit";
		private readonly string clearCommand = "clear";

		private readonly string DirectoryPath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "\\ClientFile");

		// 현재 클라이언트
		TcpClient tcpClient = null;

		private Client()
		{
			Run();
		}

		static void Main()
		{
			new Client();
		}

		// 클라이언트 메인 함수
		private void Run()
		{
			#region 다운로드를 저장할 파일 생성
			if (!Directory.Exists(DirectoryPath))
			{
				Directory.CreateDirectory(DirectoryPath);
			}
			#endregion

			while (true)
			{
				try
				{
					#region 서버 연결 및 로그인 (TcpClient가 null일 때)
					if (tcpClient is null)
					{
						// 서버 메시지 받는 문자열
						string serverMessage;

						Console.Write("서버 아이피를 입력하세요 : ");
						string ip = Console.ReadLine();

						tcpClient = new TcpClient();
						try
						{
							tcpClient.Connect(ip, 5000);

							Console.WriteLine(serverMessage = GetData());
							//GetData();
							if (serverMessage.StartsWith('E'))
							{
								tcpClient = null;

								continue;
							}

							while (true)
							{
								Console.Write("아이디 : ");
								if (Send(string.Concat("\n", Console.ReadLine())) is 1) break;
								Console.WriteLine(serverMessage = GetData());
								if (serverMessage.StartsWith('E')) continue;

								break;
							}

							while (true)
							{
								Console.Write("비밀번호 : ");
								if (Send(string.Concat("\n\n", Console.ReadLine())) is 1) break;
								Console.WriteLine(serverMessage = GetData());
								if (serverMessage.StartsWith('E')) continue;

								break;
							}
						}
						catch (SocketException)
						{
							if (ip.Equals(breakCommand)) break;

							Console.WriteLine("Error : 접속 실패");
							tcpClient = null;

							continue;
						}
						continue;
					}
					#endregion

					#region 명령어 입력
					Console.Write(">>> ");

					// 함수의 반환값이 참일 때만 서버 메시지 받기
					switch (Send(Console.ReadLine()))
					{
						case 0:
							Console.WriteLine(GetData());
							break;
						case 1:
							tcpClient.Close();
							return;
					}
					#endregion
				}
				catch (IOException)
				{
					#region 서버와의 연결이 끊겼을 때 예외 처리
					Console.WriteLine("Error : 서버와의 접속이 끊겼습니다");
					tcpClient = null;
					#endregion
				}
			}

			// while 문을 빠져나오면 클라이언트 종료
			tcpClient.Close();
		}

		private int Send(string stringData)
		{
			#region 명령어 예외 처리
			// null이면 2 반환
			if (string.IsNullOrEmpty(stringData)) return 2;

			// 콘솔 창 Clear 명령문이면 Console.Clear() 실행하고 2 반환
			if (stringData.Equals(clearCommand))
			{
				Console.Clear();
				return 2;
			}

			// 종료 명령어라면 1 반환
			if (stringData.Equals(breakCommand)) return 1;

			string[] message = stringData.Split(' ');
			if (message.Length > 2)
			{
				if (!message[0].Equals("/upload") && message.Length != 3)
				{
					Console.WriteLine("Error : 명령어 구문, 경로, 파일 이름 사이에 공백이 있으면 안됩니다");
					return 2;
				}
			}
			#endregion

			#region 데이터 서버에 전송

			if (message[0].StartsWith('\n'))
			{
				SendData(message[0]);
				return 2;
			}

			SendData(message[0]);


			#region 명령문 구별 및 실행
			switch (message[0].Substring(1))
			{
				case "fileList":
					#region 파일목록
					if (message.Length > 1)
					{
						SendData("Error");
						break;
					}
					return 0;
					#endregion
					break;
				// 클라이언트에게 파일 받기
				case "upload":
					#region 업로드
					#region 파일을 보내기 전에 에러가 없는지 확인
					if (message.Length < 2)
					{
						SendData("Error1");
						return 0;
					}
					else
					{
						SendData("Success");
					}
					GetData();

					if (!File.Exists(message[1]))
					{
						SendData("Error2");
						return 0;
					}
					else
					{
						SendData("Success");
					}
					GetData();

					string[] splitFilePath = message[1].Split('\\');
					string[] fileNames = message[1].Split('\\')[splitFilePath.Length - 1].Split(' ');
					switch (fileNames.Length)
					{
						case 1:
							if (fileNames[0].Length < 4 || fileNames[0].Substring(fileNames[0].Length - 4, 1) is not ".")
							{
								SendData("\aError1");
								return 0;
							}

							SendData(fileNames[0]);
							break;

						case 2:
							if (fileNames[0].Length < 4 || fileNames[1].Length < 4)
							{
								SendData("\aError1");
								return 0;
							}

							string[] fileExtensions =
							{
								fileNames[0].Substring(fileNames[0].Length - 4, 4),
								fileNames[1].Substring(fileNames[1].Length - 4, 4)
							};

							if (fileExtensions[0] != fileExtensions[1] || (fileExtensions[0][0] is not '.' || fileExtensions[1][0] is not '.'))
							{
								SendData("\aError2");
								return 0;
							}

							SendData(fileNames[1]);
							break;
					}
					GetData();

					if (GetData() is "Already Exists")
					{
						Console.Write("이미 파일이 있습니다. 덮어쓰시겠습니까? (Y/Any String) : ");
						string str = Console.ReadLine();
						SendData(str);

						if (str is not "Y" && str is not "y") return 2;
					}
					#endregion

					using (FileStream source = File.OpenRead(message[1]))
					{
						// 파일 크기 보내기
						BigInteger fileSize = new BigInteger(new FileInfo(message[1]).Length);
						SendData(fileSize.ToString());

						Console.WriteLine("Working : 전송 중...");
						while (true)
						{
							byte[] buffer = new byte[104857600];

							int bytesRead = source.Read(buffer, 0, buffer.Length);
							fileSize = BigInteger.Subtract(fileSize, bytesRead);
							tcpClient.GetStream().Write(buffer, 0, bytesRead);
							if (fileSize.Equals(0))
							{
								tcpClient.GetStream().Flush();
								break;
							}
						}
					}
					#endregion
					break;
				// 클라이언트에게 파일 전송
				case "download":
					#region 다운로드
					SendData(message[1]);

					string canDownload = GetData();
					if (canDownload.Equals("Error : 해당 파일이 존재하지 않습니다"))
					{
						Console.WriteLine(canDownload);
						return 2;
					}

					string name = GetData();

					using (FileStream fileStream1 = new FileStream(string.Concat(DirectoryPath, '\\', name), FileMode.OpenOrCreate, FileAccess.Write))
					{
						BigInteger fileSize = BigInteger.Parse(GetData());

						Console.WriteLine("Working : 전송 중...");
						while (true)
						{
							byte[] byteData = new byte[1048576];
							int readSize = tcpClient.GetStream().Read(byteData, 0, byteData.Length);
							fileSize = BigInteger.Subtract(fileSize, readSize);
							fileStream1.Write(byteData, 0, readSize);
							fileStream1.Flush();
							if (readSize < byteData.Length)
								break;
						}

						if (!fileSize.Equals(0))
						{
							SendData("Error");
						}
						else
						{
							SendData("Success");
						}
					}
					#endregion
					break;
			}
			#endregion

			#endregion

			// 데이터 전송 성공 시 0 반환
			return 0;
		}

		private void SendData(string message)
		{
			byte[] byteData = Encoding.UTF8.GetBytes(message, 0, message.Length);
			tcpClient.GetStream().Write(byteData, 0, byteData.Length);
			tcpClient.GetStream().Flush();
		}

		private string GetData()
		{
			#region 서버에서 데이터 전달 받고 string 으로 변환
			byte[] byteData = new byte[1024];
			int bytes = tcpClient.GetStream().Read(byteData, 0, byteData.Length);
			string message = Encoding.UTF8.GetString(byteData, 0, bytes);
			#endregion

			// 변환한 string 반환
			return message;
		}
	}
}