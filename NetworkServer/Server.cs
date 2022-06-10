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
		private const string PW = "1234";

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
			new Server();
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
			Console.WriteLine("Server Start");
			#endregion

			#region 클라이언트 연결 요청 받는 쓰레드 5개 실행
			while (true)
			{
				TcpClient client = server.AcceptTcpClient();

				if (clients.Count >= 5)
				{
					SendData("Error : 인원 수가 꽉찼습니다", client);
					client.Close();
					continue;
				}

				clients.Add(client);
				Thread thread = new Thread(() => ReceiveData(client));
				thread.Start();
			}
			#endregion
		}
		
		// 클라이언트에서 보낸 데이터 읽기
		private void ReceiveData(TcpClient tcpClient)
		{
			//ClientData callbackClient = asyncResult.AsyncState as ClientData;
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
								SendData(readString.Substring(2).Equals(PW), "Success : 비밀번호 일치", "Error : 다시 입력해주세요", callbackClient);
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
								string[] filePathArray =  Directory.GetFiles(DirectoryPath, "*.*", SearchOption.AllDirectories);

								if (filePathArray.Length is 0)
								{
									SendData("\n** 파일 없음 **\n", callbackClient.client);
								}

								string message = "\n** 파일 목록 **\n";
								for (int i = 0; i < filePathArray.Length; i++)
								{
									string[] filePathSplit = filePathArray[i].Split('\\');
									
									decimal fileLength = new FileInfo(filePathArray[i]).Length;

									string unit = string.Empty;

									int count;
									for (count = 0; (fileLength /= 1024) >= 1024 && count <= 3; count++);
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
								if (GetData(callbackClient, 1024).Equals("Error1"))
								{
									SendData("Error : 명령어가 형식에 맞지 않습니다", callbackClient.client);
									continue;
								}
								else
								{
									SendData("Success", callbackClient.client);
								}

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

								BigInteger fileSize = BigInteger.Parse(Encoding.UTF8.GetString(byteData, 0, integerLength));
								//long fileSize = long.Parse(Encoding.UTF8.GetString(byteData, 0, integerLength));
								#endregion

								#region 파일 byte 수신
								try
								{
									while (true)
									{
										byteData = new byte[1048576];
										readSize = callbackClient.client.GetStream().Read(byteData, 0, byteData.Length);
										fileSize = BigInteger.Subtract(fileSize, readSize);
										fileStream.Write(byteData, 0, readSize);
										fileStream.Flush();
										if (fileSize.Equals(0))
											break;
									}
								}
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
								string name = GetData(callbackClient, 1024);
								string downloadPath = string.Concat(DirectoryPath, '\\', name);

								if (!File.Exists(downloadPath))
								{
									SendData("Error : 해당 파일이 존재하지 않습니다", callbackClient.client);
									continue;
								}

								SendData("Success", callbackClient.client);

								using (FileStream fileStream1 = File.OpenRead(downloadPath))
								{
									// 새로운 파일 이름 보내기
									SendData(name, callbackClient.client);

									// 파일 크기 보내기
									BigInteger fileSize1 = new BigInteger(new FileInfo(downloadPath).Length);
									SendData(fileSize1.ToString(), callbackClient.client);

									try
									{
										while (true)
										{
											byte[] buffer = new byte[104857600];

											int bytesRead = fileStream1.Read(buffer, 0, buffer.Length);
											fileSize1 = BigInteger.Subtract(fileSize1, bytesRead);
											callbackClient.client.GetStream().Write(buffer, 0, bytesRead);
											if (fileSize1.Equals(0))
												break;
										}
									}
									catch (IOException)
									{
										fileStream1.Close();
										continue;
									}
								}

								if (GetData(callbackClient, 1024).Equals("Error"))
								{
									SendData("Error : 파일을 온전히 다운 받지 못하였습니다. 다시 시도해주세요", callbackClient.client);
									continue;
								}
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
			catch (IOException)
			{
				RemoveClient(tcpClient);
			}
			catch (InvalidOperationException)
			{
				RemoveClient(tcpClient);
			}
		}

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

		private string GetData(ClientData clientData, long arraySize, int readSize)
		{
			clientData.client.GetStream().Flush();
			byte[] byteData = new byte[arraySize];
			clientData.client.GetStream().Read(byteData, 0, byteData.Length);

			return Encoding.UTF8.GetString(byteData, 0, readSize);
		}
		private string GetData(ClientData clientData, long arraySize)
		{
			clientData.client.GetStream().Flush();
			byte[] byteData = new byte[arraySize];
			int readSize = clientData.client.GetStream().Read(byteData, 0, byteData.Length);

			return Encoding.UTF8.GetString(byteData, 0, readSize);
		}
		private byte[] GetData(ClientData clientData)
		{
			clientData.client.GetStream().Flush();
			byte[] byteData = new byte[1024];
			clientData.client.GetStream().Read(byteData, 0, byteData.Length);

			return byteData;
		}
	}
}