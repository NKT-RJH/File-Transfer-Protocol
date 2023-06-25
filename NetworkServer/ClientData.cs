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
