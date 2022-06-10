using System.Net.Sockets;

namespace NetworkServer
{
	internal class ClientData
	{
		public TcpClient client { get; set; }
		public byte[] readByteData { get; set; }

		public ClientData(TcpClient client)
		{
			this.client = client;
			readByteData = new byte[1024];
		}
	}
}
