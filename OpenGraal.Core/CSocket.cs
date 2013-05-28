using System;
using System.Net;
using System.Net.Sockets;

namespace OpenGraal.Core
{
	public class CSocket
	{
		/// <summary>
		/// Member Variables
		/// </summary>
		protected AsyncCallback cOnDataReceived;
		protected CString mDataIn = new CString();
		protected CString mDataOut = new CString();
		protected Encrypt mCodec = new Encrypt(Encrypt.Generation.GEN2, 0);
		protected Socket GraalSock = null;

		public Encrypt Codec
		{
			get { return mCodec; }
		}

		/// <summary>
		/// Get TCP-IP Address
		/// </summary>
		/// <returns></returns>
		public String GetIp()
		{
			if (GraalSock == null || !GraalSock.Connected)
				return String.Empty;
			return GraalSock.RemoteEndPoint.ToString();
		}

		/// <summary>
		/// Constructors
		/// </summary>
		public CSocket()
		{
			this.Init();
			this.Setup();
		}

		public void Init()
		{
			GraalSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public CSocket(Socket Sock)
		{
			GraalSock = Sock;
			this.Setup();
		}

		/// <summary>
		/// Setup Socket
		/// </summary>
		public void Setup()
		{
			// Callback Function
			cOnDataReceived = new AsyncCallback(OnDataReceived);

			// Generate Key
			mCodec.Key = (byte)(DateTime.Now.Ticks % 128);
		}

		/// <summary>
		/// Connection Status
		/// </summary>
		public bool Connected
		{
			get
			{
				return GraalSock.Connected;
			}
		}

		/// <summary>
		/// Connect to Server:Port
		/// </summary>
		public void Connect(String Hostname, Int32 Port)
		{
			if (!GraalSock.Connected)
			{
				IPHostEntry ip = Dns.GetHostEntry(Hostname);
				foreach (IPAddress addr in ip.AddressList)
				{
					if (addr.AddressFamily == AddressFamily.InterNetwork)
					{
						try
						{
							GraalSock.Connect(new IPEndPoint(addr, Port));
							break;
						} catch (Exception e)
						{
							// oops
							//Console.WriteLine(e.Message);
							//Temporär lösning.
							GraalSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
							this.Connect(Hostname, Port);
						}
					}
				}
			}
		}

		/// <summary>
		/// Disconnect Socket
		/// </summary>
		public virtual void Disconnect()
		{
			if (GraalSock.Connected)
				GraalSock.Shutdown(SocketShutdown.Both);
			GraalSock.Close();

		}

		/// <summary>
		/// Decrypt Packet
		/// </summary>
		public void DecryptPacket(CString Packet)
		{
			// Encryption Type
			Encrypt.Type EncryptType = Encrypt.Type.UNCOMPRESSED;

			// Generation Type
			switch (mCodec.Gen)
			{
			// Unencrypted Packets
				case Encrypt.Generation.GEN1:
				case Encrypt.Generation.GEN2:
					return;

			// Zlib
				case Encrypt.Generation.GEN3:
					mCodec.Apply(Encrypt.CryptType.DECRYPT, Packet, Encrypt.Type.UNCOMPRESSED);
					return;

			// BZip2
				case Encrypt.Generation.GEN4:
					EncryptType = Encrypt.Type.BZ2;
					break;

			// BZip2 / Zlib
				case Encrypt.Generation.GEN5:
					EncryptType = (Encrypt.Type)Packet.ReadByte();
					Packet.Remove(0, 1);
					break;
			}

			// Decrypt Packet
			mCodec.Apply(Encrypt.CryptType.DECRYPT, Packet, EncryptType);
            
			// Uncompress Packet
			if (EncryptType == Encrypt.Type.ZLIB)
				Packet.ZDecompress();
			else if (EncryptType == Encrypt.Type.BZ2)
				Packet.BDecompress();
		}

		/// <summary>
		/// Encrypt Packet
		/// </summary>
		public Encrypt.Type EncryptPacket(CString Packet)
		{
			// Encryption Type
			Encrypt.Type EncryptType = Encrypt.Type.UNCOMPRESSED;
			if (Packet.Length > 0x2000)
			{ // 8192
				EncryptType = Encrypt.Type.BZ2;
				Packet.BCompress();
			} else if (Packet.Length > 0x28)
			{ // 40
				EncryptType = Encrypt.Type.ZLIB;
				Packet.WriteShort(0);
				Packet.ZCompress();
			}

			// Apply Encryption
			mCodec.Limit(EncryptType);
			mCodec.Apply(Encrypt.CryptType.ENCRYPT, Packet);
			return EncryptType;
		}

		/// <summary>
		/// Send Buffer
		/// </summary>
		public Int32 Send(Byte[] Buffer)
		{
			try
			{
				return GraalSock.Send(Buffer);
			} catch (ObjectDisposedException)
			{
				return -1;
			} catch (SocketException)
			{
				this.Disconnect();
				return -1;
			}
		}

		/// <summary>
		/// Append Packet to Buffer & Send
		/// </summary>
		public void SendPacket(CString Packet)
		{
			SendPacket(Packet, true);
		}

		/// <summary>
		/// Append Packet to Buffer
		/// </summary>
		public void SendPacket(CString Packet, Boolean SendNow)
		{
			// Data Exist?
			if (Packet.Length < 1)
				return;

			// Append '\n'
			if (Packet[Packet.Length - 1] != '\n')
				Packet.WriteByte((byte)'\n');

			// Append Data
			mDataOut.Write(Packet);

			// Check Buffer & Send
			if (SendNow || mDataOut.Length > 4096)
				SendCompress();
		}

		/// <summary>
		/// Compress Data & Send Buffer
		/// </summary>
		public virtual void SendCompress()
		{
			// Data Exists?
			if (mDataOut.Length < 1)
				return;

			try
			{
				switch (mCodec.Gen)
				{
				// Uncompressed
					case Encrypt.Generation.GEN1:
						break;

				// Zlib
					case Encrypt.Generation.GEN2:
					case Encrypt.Generation.GEN3:
						mDataOut.ZCompress();
						break;

				// BZip2
					case Encrypt.Generation.GEN4:
						mCodec.Limit(Encrypt.Type.BZ2);
						mCodec.Apply(Encrypt.CryptType.ENCRYPT, mDataOut, Encrypt.Type.BZ2);
						break;

				// BZip2 / Zlib
					case Encrypt.Generation.GEN5:
						Encrypt.Type type = EncryptPacket(mDataOut);
						mDataOut.Insert(0, (Byte)type);
						break;
				}

				// Prelength Packet
				mDataOut.PreLength();

				// Send Packet
				GraalSock.Send(mDataOut.Buffer);
				mDataOut.Clear();
			} catch (ObjectDisposedException)
			{
			} catch (SocketException)
			{
				this.Disconnect();
			}
		}

		/// <summary>
		/// Event: OnDataReceived
		/// </summary>
		protected virtual void OnDataReceived(IAsyncResult AsyncResult)
		{
			try
			{
				// Write Data to Buffer
				Int32 BytesReceived = GraalSock.EndReceive(AsyncResult);
                
				if (BytesReceived > 0)
				{
					// Append Data
					mDataIn.ReadCount = 0;
					mDataIn.Write((Byte[])AsyncResult.AsyncState, BytesReceived);

					// Parse Data
					ParseData();
				}

				// Wait for Data
				ReceiveData();
			} catch (ObjectDisposedException e)
			{
				this.Disconnect();
			} catch (SocketException e)
			{
				this.Disconnect();
			}
		}

		/// <summary>
		/// Wait For Data
		/// </summary>
		public void ReceiveData()
		{
			try
			{
				Byte[] Buffer = new Byte[0xFFFF];
				GraalSock.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, cOnDataReceived, Buffer);
			} catch (ObjectDisposedException ez)
			{
				
				this.Disconnect();
			} catch (SocketException e)
			{
				Console.WriteLine("::" + e.Message + "::");
				this.Disconnect();
			}
		}

		/// <summary>
		/// Abstract Function -> Handle Data
		/// </summary>
		protected virtual void HandleData(CString Packet)
		{

		}

		protected virtual void ParseData()
		{
			// Parse Data
			while (mDataIn.Length >= 2)
			{
				// packet length
				int len = (int)mDataIn.ReadShort();
				if (len > (int)mDataIn.Length - 2)
					break;

				// grab packet
				CString packet = mDataIn.Read(len);
				mDataIn.Remove(0, len + 2);
				
				// decrypt packet
				switch (mCodec.Gen)
				{
				// Gen 1 is not encrypted or compressed.
					case Encrypt.Generation.GEN1:
						break;

				// Gen 2 and 3 are zlib compressed.  Gen 3 encrypts individual packets
				// Uncompress so we can properly decrypt later on.
					case Encrypt.Generation.GEN2:
					case Encrypt.Generation.GEN3:
						packet.ZDecompress();
						break;

				// Gen 4 and up encrypt the whole combined and compressed packet.
				// Decrypt and decompress.
					default:
						DecryptPacket(packet);
						break;
				}

				// Handle Packet
				if (packet != null)
					HandleData(packet);
			}
		}
	}
}
