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