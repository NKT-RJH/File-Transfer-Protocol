# File-Transfer-Protocol

<h4>1 대 N 형식의 서버/클라이언트 시스템으로 클라이언트에서 서버에 파일을 저장 및 가져오기를 할 수 있는 CLI 프로그램</h4>

<hr class='hr-solid'/>

<h3>시스템 구조</h3>

<details>
<summary><i>실행 이미지</i></summary>
<br>
  <img width="400" alt="image" src="https://github.com/NKT-RJH/File-Transfer-Protocol/assets/80941288/7c9b5156-38b7-4425-99d7-126878b4b9a2"><br>
  &nbsp;● 클라이언트가 서버에 접속하는 모습<br><br>
  <img width="400" alt="image" src="https://github.com/NKT-RJH/File-Transfer-Protocol/assets/80941288/435e4b0c-c2d8-4e35-ae71-7750ea9c6771"><br>
  &nbsp;● 클라이언트가 서버에 파일을 업로드하는 모습<br><br>
  <img width="400" alt="image" src="https://github.com/NKT-RJH/File-Transfer-Protocol/assets/80941288/362471dc-d0ee-4ce8-808e-56181e842025"><br>
  &nbsp;● 클라이언트가 서버의 파일을 다운로드하는 모습<br><br>
  <img width="400" alt="image" src="https://github.com/NKT-RJH/File-Transfer-Protocol/assets/80941288/258c89f0-576a-4759-9ad8-683c48041976"><br>
  &nbsp;● 클라이언트가 서버에 저장된 파일들을 목록을 확인하는 모습<br><br>
  <br>
</details>

<hr class='hr-solid'/>

<h3>주요 코드</h3>
<b>Client</b><br>
&nbsp;&nbsp;&nbsp;&nbsp;● 클라이언트 부분을 담당하는 코드입니다.<br>
&nbsp;&nbsp;&nbsp;&nbsp;● 서버와의 연결, 서버에 저장되 파일 목록 읽기, 서버에 파일 업로드, 서버에 저장된 파일 다운로드 등의 기능을 수행할 수 있습니다,
<details>
    <summary><i>자세한 코드</i></summary>
    
  ```C#
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
		// 콘솔 지우기 상수
		private readonly string clearCommand = "clear";
		// 클라이언트 파일을 저장하는 디렉토리 경로
		private readonly string DirectoryPath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "\\ClientFile");

		// 현재 클라이언트
		TcpClient tcpClient = null;

		private Client()
		{
			Run();
		}

		static void Main()
		{
			Client client = new Client();
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

						// 접속할 아이피 입력받기
						Console.Write("서버 아이피를 입력하세요 : ");
						string ip = Console.ReadLine();

						tcpClient = new TcpClient();
						try
						{
							// 입력한 아이피로 연결
							tcpClient.Connect(ip, 5000);

							Console.WriteLine(serverMessage = GetData());
							// 서버에서 Error를 보냈다면 연결 시도 중지
							if (serverMessage.StartsWith('E'))
							{
								tcpClient = null;

								continue;
							}

							while (true)
							{
								// 아이디 입력 및 송신
								Console.Write("아이디 : ");
								Send(string.Concat("\n", Console.ReadLine()));
								Console.WriteLine(serverMessage = GetData());
								// 서버에서 Error를 송신했다면 아이디가 틀렸다는 뜻이므로 반복
								if (serverMessage.StartsWith('E')) continue;

								// 아이디가 맞다면 반복문 탈출
								break;
							}

							while (true)
							{
								// 비밀번호 입력 및 송신
								Console.Write("비밀번호 : ");
								Send(string.Concat("\n\n", Console.ReadLine()));
								Console.WriteLine(serverMessage = GetData());
								// 서버에서 Error를 송신했다면 비밀번호가 틀렸다는 뜻이므로 반복
								if (serverMessage.StartsWith('E')) continue;

								// 비밀번호가 맞다면 반복문 탈출
								break;
							}
						}
						// 시간 초과 시, 접속 실패 문구 출력 후, 다시 아이피 입력을 받는다
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
			// upload 명령어가 형식에 맞지 않다면 에러 출력
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

			// 아이디와 비밀번호 데이터를 전송할 떄는 데이터 앞에 \n이 붙음
			// 만약 아이디 비밀번호 데이터라면 출력 후 2를 반환
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
				// 클라이언트에게 파일 받기
				case "upload":
					#region 업로드
					#region 파일을 보내기 전에 에러가 없는지 확인
					// 명령어 형식이 맞지 않다면 Error1 전송
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

					// 전송하려는 파일이 존재하지 않다면 Error2 전송
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
						// 기존 파일 이름 그대로 전송
						case 1:
							// 파일 이름이 1글자 미만이거나, 확장자가 없는 경우 Error1 전송
							if (fileNames[0].Length < 4 || fileNames[0].Substring(fileNames[0].Length - 4, 1) is not ".")
							{
								SendData("\aError1");
								return 0;
							}

							SendData(fileNames[0]);
							break;

						// 파일 이름을 변경하여 전송 ex) /upload abc.png(기존 파일 이름) bbb.png(변경할 파일 이름)
						case 2:
							// 파일 이름이 1글자 미만이거나, 확장자가 없는 경우 Error1 전송
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

							// 변경할 파일 이름이 기존 파일 이름과 같거나, 확장자가 없다면 Error1 전송
							if (fileExtensions[0] != fileExtensions[1] || (fileExtensions[0][0] is not '.' || fileExtensions[1][0] is not '.'))
							{
								SendData("\aError2");
								return 0;
							}

							// 파일 이름 데이터 전송
							SendData(fileNames[1]);
							break;
					}
					GetData();

					// 서버에서 이미 해당 이름의 파일이 있다고 말한다면 덮어쓰기 여부 걸졍
					if (GetData() is "Already Exists")
					{
						Console.Write("이미 파일이 있습니다. 덮어쓰시겠습니까? (Y/Any String) : ");
						string str = Console.ReadLine();
						SendData(str);

						// 덮어쓰기를 하지 않으면 upload 취소
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
							// 반복마다 최대 1048576 bit 만큼 파일 데이터 전송
							byte[] buffer = new byte[104857600];

							int bytesRead = source.Read(buffer, 0, buffer.Length);
							fileSize = BigInteger.Subtract(fileSize, bytesRead);
							tcpClient.GetStream().Write(buffer, 0, bytesRead);
							// 파일 데이터를 모두 보냈다면 반복문 탈출
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
					// 명령어 형식이 맞지 않다면 Error1 수신
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

                    SendData(message[1]);

					// 서버에서 보낸 이름에 맞는 파일이 없다고 한다면 에러 문구 출력 후 download 종료
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
							// 반복마다 최대 1048576 bit 만큼 서버가 보낸 데이터를 받아옴
							byte[] byteData = new byte[1048576];
							int readSize = tcpClient.GetStream().Read(byteData, 0, byteData.Length);
							fileSize = BigInteger.Subtract(fileSize, readSize);
							fileStream1.Write(byteData, 0, readSize);
							fileStream1.Flush();
							// 서버가 보낸 데이터를 모두 받았다면 반복문 탈출
							if (readSize < byteData.Length)
								break;
						}

						// 수신 받은 데이터의 용량이 원본과 같지 안다면 Error 전송
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

		// 서버에 데이터 전송
		private void SendData(string message)
		{
			byte[] byteData = Encoding.UTF8.GetBytes(message, 0, message.Length);
			tcpClient.GetStream().Write(byteData, 0, byteData.Length);
			tcpClient.GetStream().Flush();
		}

		// 서버한테 데이터 수신 받기
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
  ```
</details><br>

<b>Server</b><br>
&nbsp;&nbsp;&nbsp;&nbsp;● 서버 부분을 담당하는 코드입니다.<br>
&nbsp;&nbsp;&nbsp;&nbsp;● 클라이언트 연결 관리, 명령어 인식 후 명령어 수행 혹은 실패 클라이언트에 전송, 파일 데이터 저장, 파일 데이터 클라이언트에 수신 등의 기능을 수행할 수 있습니다.
<details>
    <summary><i>자세한 코드</i></summary>
    
  ```C#
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace NetworkServer
{
	internal class Server
	{
		// 아이디, 비밀번호 상수
		private const string ID = "admin";
		private const string Password = "1234";
		// 서버 파일을 저장하는 디렉토리 경로
		private readonly string DirectoryPath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "\\ServerFile");

		// 서버 전역변수
		private TcpListener server = null;
		// 클라이언트 리스트 전역변수
		private List<TcpClient> clients = new List<TcpClient>();

		private Server()
		{
			Run();
		}

		private static void Main()
		{
			Server server = new Server();
		}

		// 서버 구동 메인 함수
		private void Run()
		{
			#region 업로드를 저장할 파일 생성
			if (!Directory.Exists(DirectoryPath))
			{
				Directory.CreateDirectory(DirectoryPath);
			}
			#endregion

			#region 서버 실행
			server = new TcpListener(IPAddress.Any, 5000);
			server.Start();
			#endregion

			#region 서버 IP 출력
			Console.WriteLine("Server Start");
			Console.WriteLine("서버 IP : " + GetLocalIP());
			#endregion

			#region 클라이언트 연결 요청 받는 쓰레드 5개 실행
			while (true)
			{
				// 접속 허용
				TcpClient client = server.AcceptTcpClient();

				// 클라이언트 개수가 5개 이상이라면 접속 해제
				if (clients.Count >= 5)
				{
					SendData("Error : 인원 수가 꽉찼습니다", client);
					client.Close();
					continue;
				}
				
				// 클라이언트를 리스트에 추가하고 쓰레드를 생성하여 통신 시작
				clients.Add(client);
				Thread thread = new Thread(() => ReceiveData(client));
				thread.Start();
			}
			#endregion
		}
		
		// 클라이언트에서 보낸 데이터 읽기
		private void ReceiveData(TcpClient tcpClient)
		{
			ClientData callbackClient = new ClientData(tcpClient);
			SendData("Sucess : 접속 성공", callbackClient.client);
			try
			{
				// 콜백으로 받아온 데이터 ClientData로 형변환
				while (true)
				{
					try
					{
						#region 명령어 구별
						// 클라이언트에서 받아온 정보->UTF8->string 순으로 변환
						byte[] byteData = new byte[1024];
						int readBytes = callbackClient.client.GetStream().Read(callbackClient.readByteData, 0, callbackClient.readByteData.Length);

						string readString = Encoding.UTF8.GetString(callbackClient.readByteData, 0, readBytes);


						#region 받은 데이터의 앞에 / 가 없을 때 (아이디, 비밀번호인 경우)
						// 클라이언트에게 데이터를 받을 때 문자열 앞에 \n\n 가 있으면 비밀번호, \n가 있으면 아이디이다
						if (!readString.StartsWith('/'))
						{
							// 비밀번호인 경우
							if (readString.Substring(0, 2).Equals("\n\n"))
							{
								SendData(readString.Substring(2).Equals(Password), "Success : 비밀번호 일치", "Error : 다시 입력해주세요", callbackClient);
							}
							// 아이디인 경우
							else if (readString.Substring(0, 1).Equals("\n"))
							{
								SendData(readString.Substring(1).Equals(ID), "Success : 아이디 일치", "Error : 다시 입력해주세요", callbackClient);
							}
							// 아무 명령어에도 속하지 않은 경우
							else
							{
								SendData("Error : 명령어를 사용해주세요", callbackClient.client);
							}
							continue;
						}
						#endregion

						#region 받은 데이터에 / 가 있을 때 (명령어인 경우)

						bool isSuccess = false;

						switch (readString.Substring(1))
						{
							//저장된 파일 목록 제공
							case "fileList":
								#region 파일목록
								// 서버 디렉토리 정보 가져오기
								string[] filePathArray = Directory.GetFiles(DirectoryPath, "*.*", SearchOption.AllDirectories);

								// 서버 디렉토리에 아무것도 없다면 목록을 출력하지 않고 continue
								if (filePathArray.Length is 0)
								{
									SendData("\n** 파일 없음 **\n", callbackClient.client);
									continue;
								}

								string message = "\n** 파일 목록 **\n";
								for (int i = 0; i < filePathArray.Length; i++)
								{
									string[] filePathSplit = filePathArray[i].Split('\\');

									decimal fileLength = new FileInfo(filePathArray[i]).Length;

									string unit = string.Empty;

									// bit로 받은 파일의 용량을 1024로 나눠서 용량을 단위로 나눠서 출력
									int count;
									for (count = 0; (fileLength /= 1024) >= 1024 && count <= 3; count++) ;
									count++;
									fileLength = Math.Round(fileLength, 2);
									switch (count)
									{
										case 0:
											unit = "byte";
											break;

										case 1:
											unit = "KB";
											break;

										case 2:
											unit = "MB";
											break;

										case 3:
											unit = "GB";
											break;
									}

									message = string.Concat(message, filePathSplit[filePathSplit.Length - 1], "    ", fileLength, unit, '\n');
								}
								message = string.Concat(message, "\n");

								// 파일 목록과 각각의 용량 값 클라이언트에 전송
								SendData(message, callbackClient.client);
								#endregion
								break;
							// 클라이언트에게 파일 받기
							case "upload":
								#region 업로드
								// 데이터를 옮기고 저장할 변수 선언
								string stringData;
								FileStream fileStream;
								int readSize;

								#region 경로 예외 처리
								// 클라이언트에게 Error1을 받았다면
								if (GetData(callbackClient, 1024).Equals("Error1"))
								{
									SendData("Error : 명령어가 형식에 맞지 않습니다", callbackClient.client);
									continue;
								}
								else
								{
									SendData("Success", callbackClient.client);
								}

								// 클라이언트에게 Error2를 받았다면
								if (GetData(callbackClient, 1024).Equals("Error2"))
								{
									SendData("Error : 그런 파일이 없습니다", callbackClient.client);
									continue;
								}
								else
								{
									SendData("Success", callbackClient.client);
								}

								// 파일 경로 지정
								stringData = GetData(callbackClient, 1024);
								string path;
								switch (stringData)
								{
									case "\aError1":
										SendData("Error : 파일명에 확장자가 없으면 안됩니다", callbackClient.client);
										continue;
									case "\aError2":
										SendData("Error : 새로 지을 이름과 기존 이름의 확장자가 다르면 안됩니다", callbackClient.client);
										continue;
									default:
										path = string.Concat(DirectoryPath, "\\", stringData);
										SendData("Success", callbackClient.client);
										break;
								}
								#endregion

								#region 클라이언트에게 받은 파일이 이미 있는지 확인
								if (File.Exists(path))
								{
									// 이미 파일이 있다고 전송
									SendData("Already Exists", callbackClient.client);

									// 덮어쓸건지 취소할건지 응답 받고 동작
									stringData = GetData(callbackClient, 1024, 1);

									// Y 나 y 가 아니라면 모두 취소로 판단
									if (!(stringData.Equals("Y") || stringData.Equals("y"))) continue;
									// 취소가 아니라서 goto 문이 실행되지 않았다면 파일 덮어쓰기 실행
									fileStream = new FileStream(path, FileMode.Open, FileAccess.Write);
								}
								else
								{
									SendData("Success", callbackClient.client);
									fileStream = File.Create(path);
								}
								#endregion

								#region 파일 크기 수신
								byteData = GetData(callbackClient);

								int integerLength = 0;
								for (integerLength = 0; byteData[integerLength] >= 48 && byteData[integerLength] <= 57; integerLength++) ;

								// long으로도 저장을 할 수 없기에 BigInteger로 용량 정보 값을 수신
								BigInteger fileSize = BigInteger.Parse(Encoding.UTF8.GetString(byteData, 0, integerLength));
								#endregion

								#region 파일 byte 수신
								try
								{
									while (true)
									{
										// 반복마다 최대 1048576 bit 만큼 클라이언트가 보낸 데이터를 받아옴
										byteData = new byte[1048576];
										readSize = callbackClient.client.GetStream().Read(byteData, 0, byteData.Length);
										fileSize = BigInteger.Subtract(fileSize, readSize);
										fileStream.Write(byteData, 0, readSize);
										fileStream.Flush();
										// 클라이언트가 보낸 데이터를 모두 받았다면 반복문 탈출
										if (fileSize.Equals(0))
											break;
									}
								}
								// 예외 발생 시, 수신하던 파일 삭제 및 FileStream 종료
								catch (IOException)
								{
									fileStream.Close();
									File.Delete(path);
									continue;
								}
								#endregion

								fileStream.Close();

								isSuccess = true;
								#endregion
								break;
							// 클라이언트에게 파일 전송
							case "download":
								#region 다운로드
								// 클라이언트에게 Error1을 받았다면
								if (GetData(callbackClient, 1024).Equals("Error1"))
                                {
                                    SendData("Error : 명령어가 형식에 맞지 않습니다", callbackClient.client);
                                    continue;
                                }
                                else
                                {
                                    SendData("Success", callbackClient.client);
                                }

								// 클라이언트에서 다운로드 받고 싶어하는 파일 이름을 수신 후 경로로 만듦
								string name = GetData(callbackClient, 1024);
								string downloadPath = string.Concat(DirectoryPath, '\\', name);

								// 해당 이름의 파일이 존재한다면 Success, 존재하지 않다면 Error 전송
								if (!File.Exists(downloadPath))
								{
									SendData("Error : 해당 파일이 존재하지 않습니다", callbackClient.client);
									continue;
								}
								else
								{
									SendData("Success", callbackClient.client);
								}

								// 파일을 열어서 정보를 클라이언트에 수신
								using (FileStream fileStream1 = File.OpenRead(downloadPath))
								{
									// 파일 이름 보내기
									SendData(name, callbackClient.client);

									// 파일 크기 보내기
									BigInteger fileSize1 = new BigInteger(new FileInfo(downloadPath).Length);
									SendData(fileSize1.ToString(), callbackClient.client);

									try
									{
										while (true)
										{
											// 반복마다 최대 1048576 bit 만큼 파일 데이터를 전송
											byte[] buffer = new byte[104857600];

											int bytesRead = fileStream1.Read(buffer, 0, buffer.Length);
											fileSize1 = BigInteger.Subtract(fileSize1, bytesRead);
											callbackClient.client.GetStream().Write(buffer, 0, bytesRead);
											// 파일 데이터를 모두 보냈다면 반복문 탈출
											if (fileSize1.Equals(0))
												break;
										}
									}
									// 예외 발생 시, FileStream 종료
									catch (IOException)
									{
										fileStream1.Close();
										continue;
									}
								}

								// 클라이언트에서 파일이 온전히 전송되지 않았다는 Error를 수신 받았다면, 에러 문구 클라이언트에 전송
								if (GetData(callbackClient, 1024).Equals("Error"))
								{
									SendData("Error : 파일을 온전히 다운 받지 못하였습니다. 다시 시도해주세요", callbackClient.client);
									continue;
								}
								// 아니라면 성공으로 간주
								else
								{
									isSuccess = true;
								}
								#endregion
								break;
							// 아무 명령어에도 속하지 않은 경우
							default:
								isSuccess = false;
								break;
						}

						// 명령어 수행 여부 메시지 전송
						SendData(isSuccess, "Success : 명령어 수행 성공", "Error : 형식에 맞는 명령어인지 확인해주세요", callbackClient);

						#endregion
						#endregion
					}
					catch (ArgumentOutOfRangeException)
					{
						#region 클라이언트에서 과도한 명령어 전달 시 예외 처리
						SendData("Warning : 과도한 데이터 전달", callbackClient.client);
						#endregion
					}
				}
			}
			// 부가적인 에러 발생 시, 클라이언트와의 연결 종료
			catch (IOException)
			{
				RemoveClient(tcpClient);
			}
			catch (InvalidOperationException)
			{
				RemoveClient(tcpClient);
			}
		}

		// 리스트에 저장된 클라이언트 제거
		private void RemoveClient(TcpClient tcpClient)
		{
			for (int count = 0; count < clients.Count; count++)
			{
				// 연결이 해지되었다면 해지된 클라이언트를 찾아서 삭제하고 다시 클라이언트 탐색
				if (clients[count].Equals(tcpClient))
				{
					clients.RemoveAt(count);
				}
			}
		}

		#region 클라이언트에 데이터 전송
		// 조건에 따라 string 데이터 byte 배열로 변환
		private void SendData(bool result, string successMessage, string failedMessage, ClientData clientData)
		{
			byte[] byteData = Encoding.UTF8.GetBytes(result ? successMessage : failedMessage);
			clientData.client.GetStream().Write(byteData, 0, byteData.Length);
			clientData.client.GetStream().Flush();
		}
		// string 데이터 byte 배열로 변환
		private byte[] SendData(string message, TcpClient tcpClient)
		{
			byte[] byteData = Encoding.UTF8.GetBytes(message);
			tcpClient.GetStream().Write(byteData, 0, byteData.Length);
			tcpClient.GetStream().Flush();

			return byteData;
		}
		#endregion

		#region 클라이언트에게서 데이터 수신
		// 정해진 단위(arraySize)로 수신받고 수신받은 데이터를 정해진 단위(readSize)로 UTF8 형식 string으로 변환
		private string GetData(ClientData clientData, long arraySize, int readSize)
		{
			clientData.client.GetStream().Flush();
			byte[] byteData = new byte[arraySize];
			clientData.client.GetStream().Read(byteData, 0, byteData.Length);

			return Encoding.UTF8.GetString(byteData, 0, readSize);
		}
		// 정해진 단위(arraySize)로 클라이언트에게서 데이터를 수신
		private string GetData(ClientData clientData, long arraySize)
		{
			clientData.client.GetStream().Flush();
			byte[] byteData = new byte[arraySize];
			int readSize = clientData.client.GetStream().Read(byteData, 0, byteData.Length);

			return Encoding.UTF8.GetString(byteData, 0, readSize);
		}
		// 1024 단위로 클라이언트에게서 데이터를 수신
		private byte[] GetData(ClientData clientData)
		{
			clientData.client.GetStream().Flush();
			byte[] byteData = new byte[1024];
			clientData.client.GetStream().Read(byteData, 0, byteData.Length);

			return byteData;
		}
		#endregion

		// 서버 컴퓨터의 IP를 가져오는 함수
		private string GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            string LocalIP = string.Empty;

            for (int i = 0; i < host.AddressList.Length; i++)
            {
                if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    LocalIP = host.AddressList[i].ToString();
                    break;
                }
            }

            return LocalIP;
        }
    }
}
  ```
</details><br>

<b>ClientData</b><br>
&nbsp;&nbsp;&nbsp;&nbsp;● 서버에 연결된 클라이언트를 관리하기 쉽도록 만든 클래스입니다.
<details>
    <summary><i>자세한 코드</i></summary>
    
  ```C#
using System.Net.Sockets;

namespace NetworkServer
{
	internal class ClientData
	{
		// 클라이언트 정보
		public TcpClient client { get; set; }
		// 서버에서 수신 시, 한 번에 가져오는 byte 양
		public byte[] readByteData { get; set; }

		// 생성 시, 클래스 초기화
		public ClientData(TcpClient client)
		{
			this.client = client;
			readByteData = new byte[1024];
		}
	}
}

  ```
</details>
