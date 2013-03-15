using System;
using System.Text;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenGraal;
using MySql.Data.MySqlClient;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace OpenGraal.Core
{
	/// <summary>
	/// Class for DeepCloning
	/// </summary>
	public class THData
	{
		public virtual THData DeepClone()
		{
			THData returnData = (THData)this.MemberwiseClone();
			Type type = returnData.GetType();
			FieldInfo[] fieldInfoArray = type.GetFields();

			foreach (FieldInfo fieldInfo in fieldInfoArray)
			{
				object sourceFieldValue = fieldInfo.GetValue(this);
				if (sourceFieldValue is THData)
				{
					THData sourceTHData = (THData)sourceFieldValue;
					THData clonedTHData = sourceTHData.DeepClone();
					fieldInfo.SetValue(returnData, clonedTHData);
				}
			}

			return returnData;
		}
	}

	public class CString : THData
	{
		/// <summary>
		/// Member Variables
		/// </summary>
		Int32 mReadCount = 0;
		List<Byte> mBuffer = new List<Byte>();


		public CString(string buffer = "")
		{
			this.Write(Encoding.Default.GetBytes(buffer));
		}

		/// <summary>
		/// Operator Function -> [INDEX]
		/// </summary>
		public Byte this[Int32 Index]
		{
			get
			{
				return (Index >= 0 && Index <= mBuffer.Count - 1 ? mBuffer[Index] : (Byte)0);
			}
			set
			{
				if (Index >= 0 && Index <= mBuffer.Count - 1)
					mBuffer[Index] = value;
			}
		}

		/// <summary>
		/// Return Byte-Array of Buffer
		/// </summary>
		public Byte[] Buffer
		{
			get
			{
				return mBuffer.ToArray();
			}
		}

		public string Escape()
		{

			return MySqlHelper.EscapeString(this.Text);
		}

		/// <summary>
		/// Return Bytes Left (Unread Bytes)
		/// </summary>
		public Int32 BytesLeft
		{
			get
			{
				return Math.Max(0, Length - ReadCount);
			}
		}

		/// <summary>
		/// Return Capacity of Buffer
		/// </summary>
		public Int32 Capacity
		{
			get
			{
				return mBuffer.Capacity;
			}
			set
			{
				mBuffer.Capacity = value;
			}
		}

		/// <summary>
		/// Return Length of Buffer
		/// </summary>
		public Int32 Length
		{
			get
			{
				return mBuffer.Count;
			}
		}

		/// <summary>
		/// Return/Set Currently Read Bytes
		/// </summary>
		public Int32 ReadCount
		{
			get
			{
				return mReadCount;
			}
			set
			{
				mReadCount = Math.Max(Math.Min(value, mBuffer.Count), 0);
			}
		}

		/// <summary>
		/// Return Text-Encoding of Buffer
		/// </summary>
		public String Text
		{
			get
			{
				return Encoding.Default.GetString(mBuffer.ToArray(), 0, Length);
			}
		}

		/// <summary>
		/// Clear current CString
		/// </summary>
		public CString Clear()
		{
			mBuffer.Clear();
			ReadCount = 0;
			return this;
		}

		/// <summary>
		/// Insert Byte[] into Buffer
		/// </summary>
		public CString Insert(Int32 Start, Byte Byte)
		{
			mBuffer.Insert(Start, Byte);
			return this;
		}

		/// <summary>
		/// Insert Byte[] Array into Buffer
		/// </summary>
		public CString Insert(Int32 Start, Byte[] Data)
		{
			mBuffer.InsertRange(Start, Data);
			return this;
		}

		/// <summary>
		/// Remove 'Count' from 'Start' Bytes
		/// </summary>
		public CString Remove(Int32 Start, Int32 Count)
		{
			// Count Check
			if (Count < 0)
				return this;

			// Remove Data
			mBuffer.RemoveRange(0, Math.Min(Count - Start, Length - Start));
			ReadCount = 0;
			return this;
		}

		/// <summary>
		/// Read Data from Buffer into Byte[]
		/// </summary>
		public void Read(byte[] Data)
		{
			int Count = Math.Min(Data.Length, Length - ReadCount);
			for (int i = 0; i < Count; i++)
				Data[i] = mBuffer[ReadCount++];
		}

		/// <summary>
		/// Read Data from Buffer into CString
		/// </summary>
		public CString Read(int Count)
		{
			CString Data = new CString();
			Read(Data, Count);
			return Data;
		}

		/// <summary>
		/// Read Data from Buffer into CString
		/// </summary>
		public void Read(CString Data, int Count)
		{
			if (Count < 1)
				return;

			Count = Math.Min(Count, Length - ReadCount);
			for (int i = 0; i < Count; i++)
				Data.WriteByte(mBuffer[ReadCount++]);
		}

		/// <summary>
		/// Write Full Data to Buffer
		/// </summary>
		public void Write(byte[] Data)
		{
			Write(Data, Data.Length);
		}

		/// <summary>
		/// Write Data to Buffer (Count)
		/// </summary>
		public void Write(byte[] Data, int Count)
		{
			Count = Math.Min(Data.Length, Count);
			for (int i = 0; i < Count; i++)
				mBuffer.Add(Data[i]);
		}

		/// <summary>
		/// Write CString to Buffer
		/// </summary>
		public void Write(CString Data)
		{
			Write(Data.Buffer, Data.Length);
		}

		/// <summary>
		/// Write String to Buffer
		/// </summary>
		public void Write(string Data)
		{
			this.Write(Encoding.Default.GetBytes(Data));
		}

		/// <summary>
		/// Find Position of 'Character' in Buffer (Start Position: 0)
		/// </summary>
		public Int32 IndexOf(Char Char)
		{
			return IndexOf(Char, 0);
		}

		/// <summary>
		/// Find Position of 'Character' in Buffer
		/// </summary>
		public Int32 IndexOf(Char Char, Int32 Start)
		{
			if (Start < 0 || Start > Length)
				return -1;

			for (int i = Start; i < Length; i++)
			{
				if (mBuffer[i] == (byte)Char)
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Prefix Length of CString
		/// </summary>
		public CString PreLength()
		{
			Byte[] data = new Byte[2];
			data[0] = (Byte)((Length >> 8) & 0xFF);
			data[1] = (Byte)(Length & 0xFF);
			Insert(0, data);
			return this;
		}

		/// <summary>
		/// Compression through bzip2
		/// </summary>
		public CString BCompress()
		{
			// Do Compression
			MemoryStream InStream = new MemoryStream(mBuffer.ToArray());
			MemoryStream OutStream = new MemoryStream();
			BZip2.Compress(InStream, OutStream, true, 9);

			// Recreate Buffer
			mBuffer = OutStream.ToArray().ToList();
			return this;
		}

		/// <summary>
		/// Decompression through bzip2
		/// </summary>
		public CString BDecompress()
		{
			// Do Decompression
			MemoryStream InStream = new MemoryStream(mBuffer.ToArray());
			MemoryStream OutStream = new MemoryStream();
			BZip2.Decompress(InStream, OutStream, true);

			// Recreate Buffer
			mBuffer = OutStream.ToArray().ToList();
			return this;
		}

		/// <summary>
		/// Compression through zlib
		/// </summary>
		public CString ZCompress()
		{
			// Do Compression
			MemoryStream MemStream = new MemoryStream();
			DeflaterOutputStream DefStream = new DeflaterOutputStream(MemStream, new Deflater(Deflater.DEFAULT_COMPRESSION, false));
			DefStream.Write(mBuffer.ToArray(), 0, mBuffer.Count);
			DefStream.Finish();
			DefStream.Close();

			// Recreate Buffer
			mBuffer = MemStream.ToArray().ToList();
			return this;
		}

		/// <summary>
		/// Decompression through zlib
		/// </summary>
		public CString ZDecompress()
		{
			// Do Compression
			byte[] newBuff = new byte[204800];
			Inflater I = new Inflater();
			I.SetInput(mBuffer.ToArray(), 0, mBuffer.Count);
			I.Inflate(newBuff, 0, newBuff.Length);

			// Recreate Buffer
			mBuffer = newBuff.ToList();
			mBuffer.RemoveRange((int)(I.TotalOut), (int)(newBuff.Length - I.TotalOut));
			return this;
		}

		public SByte ReadByte()
		{
			Byte[] data = new byte[1];
			Read(data);
			return (sbyte)data[0];
		}

		public Int16 ReadShort()
		{
			Byte[] data = new byte[2];
			Read(data);
			return (Int16)(((data[0]) << 8) + data[1]);
		}

		public Int32 ReadInt()
		{
			Byte[] data = new byte[4];
			Read(data);
			return (int)(((data[0]) << 24) + ((data[1]) << 16) + ((data[2]) << 8) + data[3]);
		}

		public String ReadChars(int pCount)
		{
			Byte[] data = new byte[pCount];
			Read(data);
			return Encoding.Default.GetString(data, 0, data.Length);
		}

		public CString ReadChars2(uint pCount)
		{
			Byte[] data = new byte[pCount];
			Read(data);
			CString data2 = new CString();

			data2.Write(Encoding.Default.GetString(data, 0, data.Length));
			return data2;
		}

		public CString ReadString()
		{
			CString data = new CString();
			Read(data, Length - ReadCount);
			return data;
		}

		public CString ReadString(Char Item)
		{
			int pos = IndexOf(Item, ReadCount) - ReadCount;
			CString data = new CString();
			Read(data, (pos >= 0 ? pos : Length - ReadCount));
			ReadCount++;
			return data;
		}

		public void WriteByte(Byte Byte)
		{
			mBuffer.Add(Byte);
		}

		public void WriteShort(Int16 Byte)
		{
			Byte[] data = new Byte[2];
			data[0] = (Byte)((Byte >> 8) & 0xFF);
			data[1] = (Byte)(Byte & 0xFF);
			Write(data);
		}

		public void WriteInt3(Int32 Byte)
		{
			Byte[] data = new Byte[3];
			data[0] = (Byte)((Byte >> 16) & 0xFF);
			data[1] = (Byte)((Byte >> 8) & 0xFF);
			data[2] = (Byte)(Byte & 0xFF);
			Write(data);
		}

		public void WriteInt(Int32 Byte)
		{
			Byte[] data = new Byte[4];
			data[0] = (Byte)((Byte >> 24) & 0xFF);
			data[1] = (Byte)((Byte >> 16) & 0xFF);
			data[2] = (Byte)((Byte >> 8) & 0xFF);
			data[3] = (Byte)(Byte & 0xFF);
			Write(data);
		}

		public void WriteLong(Int64 Byte)
		{
			Byte[] data = new Byte[5];
			data[0] = (Byte)((Byte >> 32) & 0xFF);
			data[1] = (Byte)((Byte >> 24) & 0xFF);
			data[2] = (Byte)((Byte >> 16) & 0xFF);
			data[3] = (Byte)((Byte >> 8) & 0xFF);
			data[4] = (Byte)(Byte & 0xFF);
			Write(data);
		}

		// Read Signed Graal-Byte Data
		public sbyte ReadGByte1()
		{
			byte[] data = new byte[1];
			Read(data);
			return (sbyte)(data[0] - 32);
		}

		public short ReadGByte2()
		{
			byte[] data = new byte[2];
			Read(data);
			return (short)(((data[0] - 32) << 7) + data[1] - 32);
		}

		public int ReadGByte3()
		{
			byte[] data = new byte[3];
			Read(data);
			return (int)(((data[0] - 32) << 14) + ((data[1] - 32) << 7) + data[2] - 32);
		}

		public int ReadGByte4()
		{
			byte[] data = new byte[4];
			Read(data);
			return (int)(((data[0] - 32) << 21) + ((data[1] - 32) << 14) + ((data[2] - 32) << 7) + data[3] - 32);
		}

		public int ReadGByte5()
		{
			byte[] data = new byte[5];
			Read(data);
			return (int)(((data[0] - 32) << 28) + ((data[1] - 32) << 21) + ((data[2] - 32) << 14) + ((data[3] - 32) << 7) + data[4] - 32);
		}

		// Read Unsigned Graal-Byte Data
		public byte ReadGUByte1()
		{
			return (byte)ReadGByte1();
		}

		public ushort ReadGUByte2()
		{
			return (ushort)ReadGByte2();
		}

		public uint ReadGUByte3()
		{
			return (uint)ReadGByte3();
		}

		public uint ReadGUByte4()
		{
			return (uint)ReadGByte4();
		}

		public uint ReadGUByte5()
		{
			return (uint)ReadGByte5();
		}

		// Write Graal-Byte Data
		public CString WriteGByte1(sbyte pByte)
		{
			byte[] data = new byte[1];
			data[0] = (byte)(pByte + 32);
			Write(data);
			return this;
		}

		public CString WriteGByte2(short pByte)
		{
			byte[] data = new byte[2];
			data[0] = (byte)(((pByte >> 7) & 0x7F) + 32);
			data[1] = (byte)((pByte & 0x7F) + 32);
			Write(data);
			return this;
		}

		public CString WriteGByte3(int pByte)
		{
			byte[] data = new byte[3];
			data[0] = (byte)(((pByte >> 14) & 0x7F) + 32);
			data[1] = (byte)(((pByte >> 7) & 0x7F) + 32);
			data[2] = (byte)((pByte & 0x7F) + 32);
			Write(data);
			return this;
		}

		public CString WriteGByte4(int pByte)
		{
			byte[] data = new byte[4];
			data[0] = (byte)(((pByte >> 21) & 0x7F) + 32);
			data[1] = (byte)(((pByte >> 14) & 0x7F) + 32);
			data[2] = (byte)(((pByte >> 7) & 0x7F) + 32);
			data[3] = (byte)((pByte & 0x7F) + 32);
			Write(data);
			return this;
		}

		public CString WriteGByte5(long pByte)
		{
			byte[] data = new byte[5];
			data[0] = (byte)(((pByte >> 28) & 0x7F) + 32);
			data[1] = (byte)(((pByte >> 21) & 0x7F) + 32);
			data[2] = (byte)(((pByte >> 14) & 0x7F) + 32);
			data[3] = (byte)(((pByte >> 7) & 0x7F) + 32);
			data[4] = (byte)((pByte & 0x7F) + 32);
			Write(data);
			return this;
		}

		public static CString operator +(CString x, CString y)
		{
			x.Write(y);
			return x;
		}

		public static CString operator +(CString x, byte y)
		{
			return x.WriteGByte1((sbyte)y);
		}

		public static CString operator +(CString x, short y)
		{
			return x.WriteGByte2(y);
		}

		public static CString operator +(CString x, int y)
		{
			return x.WriteGByte3(y);
		}

		public static CString operator +(CString x, long y)
		{
			return x.WriteGByte5(y);
		}

		public static CString operator +(CString x, string y)
		{
			x.Write(y);
			return x;
		}

		// Graal-Tokenize String
		static public String tokenize(String pString)
		{
			// Definition
			Int32[] pos = new Int32[2] { 0, 0 };
			String retVal = String.Empty;

			// Append '\n' to line
			if (pString[pString.Length - 1] != '\n')
				pString += '\n';

			// Tokenize String
			while ((pos[0] = pString.IndexOf('\n', pos[1])) != -1)
			{
				String temp = pString.Substring(pos[1], pos[0] - pos[1]);
				temp = temp.Replace("\"", "\"\"");
				temp = temp.Replace("\r", "");
				retVal += (temp.Length != 0 ? "\"" + temp + "\"," : ",");
				pos[1] = pos[0] + 1;
			}

			// Kill the trailing comma and return our new string.
			return retVal.Remove(retVal.Length - 1, 1); ;
		}

		// Graal-Untokenize String
		static public String untokenize(String pString)
		{
			// Definition
			Int32[] pos = new Int32[2] { 0, 1 };
			String retVal = String.Empty;
			List<String> temp = new List<String>();

			// Trim Buffer
			pString = pString.Trim();

			// Check to see if it starts with a quotation mark.  If not, set pos[1] to 0.
			if (pString.Length > 0)
			{
				if (pString[0] != '\"') pos[1] = 0;

				// Untokenize.
				while ((pos[0] = pString.IndexOf(",", pos[1])) != -1)
				{
					// Empty blocks are blank lines.
					if (pos[0] == pos[1])
					{
						pos[1]++;
						temp.Add("\r");
						continue;
					}

					// ,"", blank lines.
					if (pos[0] - pos[1] == 1 && pString[pos[1]] == '\"')
					{
						pos[1] += 3;
						temp.Add("\r");
						continue;
					}

					// Check for ,,"""blah"
					if (pString[pos[1]] == '\"' && pString[pos[1] + 1] != '\"')
					{
						// Check to make sure it isn't ,"",
						if (!(pos[1] + 2 < pString.Length && pString[pos[1] + 2] == ','))
							pos[1]++;
					}

					// Check and see if the comma is outside or inside of the thing string.
					// If pos[1] points to a quotation mark we have to find the closing quotation mark.
					if (pos[1] > 0 && pString[pos[1] - 1] == '\"')
					{
						while (true)
						{
							if (pos[0] == -1) break;
							if ((pString[pos[0] - 1] != '\"') || (pString[pos[0] - 1] == '\"' && pString[pos[0] - 2] == '\"'))
								pos[0] = pString.IndexOf(",", pos[0] + 1);
							else
								break;
						}
					}

					// Exit out if we previously failed to find the end.
					if (pos[0] == -1) break;

					// "test",test
					String t2 = String.Empty;
					if (pos[0] > 0 && pString[pos[0] - 1] == '\"')
						t2 = pString.Substring(pos[1], pos[0] - pos[1] - 1);
					else
						t2 = pString.Substring(pos[1], pos[0] - pos[1]);

					// Check if the string is valid and if it is, Add it.
					temp.Add(t2.Replace("\"\"", "\""));

					// Move forward the correct number of spaces.
					if (pos[0] + 1 != pString.Length && pString[pos[0] + 1] == '\"')
						pos[1] = pos[0] + 2;	// test,"test
					else
						pos[1] = pos[0] + 1;	// test,test
				}

				// Try and grab the very last element.
				if (pos[1] < pString.Length)
				{
					// If the end is a quotation mark, remove it.
					if (pString[pString.Length - 1] == '\"')
						pString = pString.Remove(pString.Length - 1, 1);

					// Sanity check.
					if (pos[1] != pString.Length)
						temp.Add(pString.Substring(pos[1]).Replace("\"\"", "\"")); // Replace "" with "
				}

				// Write the correct string out.
				foreach (String x in temp)
					retVal += x + System.Environment.NewLine;

				// Reset old value & return
				pString = retVal;
				return retVal;
			}
			return pString;
		}
	}
}
