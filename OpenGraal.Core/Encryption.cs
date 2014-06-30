using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace OpenGraal.Core
{
	public class Encrypt
	{
		/// <summary>
		/// Encryption Generation
		/// </summary>
		public enum Generation
		{
			GEN1 = 0,
			// No encryption/no compression.
			GEN2 = 1,
			// No encryption/zlib compression.
			GEN3 = 2,
			// Single byte insertion/zlib compression.
			GEN4 = 3,
			// Partial packet encryption/bz2 compression.
			GEN5 = 4,
			// Partial packet encryption/none, zlib, bz2 compression methods.
			GEN6 = 5,
			// Unknown (Graal v6 encryption).
		};

		/// <summary>
		/// Encryption Type
		/// </summary>
		public enum Type
		{
			UNCOMPRESSED = 0x02,
			ZLIB = 0x04,
			BZ2 = 0x06,
		};

		/// <summary>
		/// Crypt Type
		/// </summary>
		public enum CryptType
		{
			DECRYPT	= 0,
			ENCRYPT	= 1,
		};

		/// <summary>
		/// Encryption Key
		/// </summary>
		public Byte Key
		{
			get { return mKey; }
			set { mKey = value; }
		}

		/// <summary>
		/// Encryption Generation
		/// </summary>
		public Generation Gen
		{
			get { return mGeneration; }
			set { mGeneration = value; }
		}

		/// <summary>
		/// Member Variables
		/// </summary>
		static readonly UInt32[] ITERATOR_START_VAL = new UInt32[] { 0, 0, 0x04A80B38, 0x4A80B38, 0x4A80B38, 0 };
		Byte mKey = 0;
		Int32 mLimit = -1;
		UInt32[] mIterator = new UInt32[] { 0, 0 };
		Generation mGeneration = 0;

		/// <summary>
		/// Constructor: Encryption ( Generation, Key )
		/// </summary>
		public Encrypt(Generation Gen, Byte Key)
		{
			this.Reset(Gen, Key);
		}

		/// <summary>
		/// Apply Encryption
		/// </summary>
		public void Apply(CryptType Crypt, CString Buffer)
		{
			// Apply the correct decryption algorithm
			switch (mGeneration)
			{
			// No Encryption
				case Generation.GEN1:
				case Generation.GEN2:
					return;

			// Single byte insertion/zlib compression
				case Generation.GEN3:
					mIterator[(Int32)Crypt] *= 0x8088405;
					mIterator[(Int32)Crypt] += mKey;
					Buffer.Remove((((Int32)mIterator[(Int32)Crypt] & 0x0FFFF) % Buffer.Length), 1);
					break;

			// Partial packet encryption/none, zlib, bz2 compression methods.
			// Gen 4 is only bz2.
				case Generation.GEN4:
				case Generation.GEN5:
					for (int i = 0; i < Buffer.Length; i++)
					{
						if (i % 4 == 0)
						{
							if (mLimit == 0)
								return;

							mIterator[(Int32)Crypt] *= 0x8088405;
							mIterator[(Int32)Crypt] += mKey;
							if (mLimit > 0)
								mLimit--;
						}

						Buffer[i] ^= (Byte)((mIterator[(Int32)Crypt] >> ((i % 4) * 8)) & 0xFF);
					}
					break;

			// Future Encryption Method (Graal V6)
				case Generation.GEN6:
					break;
			}
		}

		/// <summary>
		/// Convenience Function -> Limit/Apply Encryption
		/// </summary>
		public void Apply(CryptType Crypt, CString Buffer, Type Type)
		{
			Limit(Type);
			Apply(Crypt, Buffer);
		}

		/// <summary>
		/// Limit Encryption
		/// </summary>
		public bool Limit(Type Type)
		{
			int[,] limits = new int[,] { { 0x02, 0x0C }, { 0x04, 0x04 }, { 0x06, 0x04 } };
			for (int i = 0; i < limits.Length; i++)
			{
				if (limits[i, 0] == (int)Type)
				{
					this.mLimit = limits[i, 1];
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Reset Encryption ( Generation )
		/// </summary>
		public void Reset(Generation Gen)
		{
			this.Reset(Gen, mKey);
		}

		/// <summary>
		/// Reset Encryption ( Generation, Key )
		/// </summary>
		public void Reset(Generation Gen, Byte Key)
		{
			this.mKey = Key;
			this.mGeneration = Gen;
			this.mIterator[0] = ITERATOR_START_VAL[(int)this.mGeneration];
			this.mIterator[1] = ITERATOR_START_VAL[(int)this.mGeneration];
			this.mLimit = -1;
		}
	}
}
