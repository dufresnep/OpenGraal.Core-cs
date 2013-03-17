using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGraal.Core
{
	public class CStringList : IEnumerable
	{
		#region Member Variables
		/// <summary>
		/// Player-List
		/// </summary>
		private Dictionary<int, CString> _bufferList = new Dictionary<int, CString>();
		protected Int32 _position = -1;
		protected int _id = 0;
		#endregion

		#region Constructor / Destructor
		public CStringList()
		{
		}

		~CStringList()
		{
			this.Clear();
		}
		#endregion

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Iterate Playerlist Manager
		/// </summary>
		public IEnumerator<CString> GetEnumerator()
		{
			foreach (KeyValuePair<int, CString> Player in this._bufferList)
				yield return Player.Value;
		}

		#region Public functions
		public int Add(string pStr)
		{
			CString newString;
			if(pStr == null)
				newString = new CString();
			else
			{
				newString = new CString();
				newString.Write(pStr);
			}
			_bufferList[this._id] = newString;
			int listId = this._id;
			this._id++;
			return listId;
		}

		public int Add(CString pString)
		{
			CString newString = new CString();
			newString.Write(pString);
			_bufferList[this._id] = newString;
			int listId = this._id;
			this._id++;
			return listId;
		}
		public CString Get(int index)
		{
			/*
			Console.WriteLine("Index: " + index);
			foreach (KeyValuePair<int,CString> buff in this._bufferList)
				Console.WriteLine(buff.Key + " " + buff.Value.Text);
			*/
			return this._bufferList[index];
		}

		public void Remove(int pIndex)
		{
			CString str = (CString)_bufferList[pIndex];
			if(str == null)
				return;
			//str.Remove();
			_bufferList.Remove(pIndex);
		}

		public void Clear()
		{
			if(this._bufferList.Count <= 0)
				return;
			
			for(int i = 0; i < this._bufferList.Count; i++)
				this._bufferList.Remove(i);

			this._bufferList.Clear();
		}

		public CString Item(int pIndex)
		{
			return (CString)_bufferList[pIndex];
		}

		public void Replace(int pIndex, string pString)
		{
			CString str = (CString)_bufferList[pIndex];
			if (str == null)
				return;
			//delete str;
			
			str = new CString();
			str.Write(pString);
			this._bufferList[pIndex] = str;
		}

		public bool Load(string pFileName)
		{
			string[] lines = System.IO.File.ReadAllLines(pFileName);

			this.Clear();

			if(lines.Length == 0)
				return false;

			//Read each line and add it to list
			foreach(string line in lines)
			{
				//Trim end of line (spaces allowed)
				string tmpLine = line;

				for(int i = (int)line.Length-1; i >= 0; i--)
				{
					if(line.Length < 1)
						tmpLine = "\0";
					else break;
				}

				this.Add(tmpLine.TrimEnd());
			}

			return true;
		}

		public void Load(string pInput, char pSep)
		{
			CString newString = new CString();
			//this.clear();
			newString.Write(pInput);
			
			string[] lines = newString.Text.Split(pSep);
			foreach(string line in lines)
			{
				this.Add(line);
				//line = strtok(null, pSep);
			}
		}

		public void Load(string[] pStrings, int pCount)
		{
			this.Clear();
			for(int i = 0; i < pCount; i++)
				Add(pStrings[i]);
		}

		public int find(CString pString)
		{
			for(int i = 0; i < this._bufferList.Count; i++)
			{
				if(this._bufferList[i] == pString)
					return i;
			}
			return -1;
		}

		public int FindI(string pString)
		{
			for (int i = 0; i < this._bufferList.Count; i++)
			{
				if (this._bufferList[i].Text.IndexOf(pString) == 0)
					return i;
			}
			return -1;
		}

		public void Save(string pFileName)
		{
			StreamWriter file = new StreamWriter(pFileName, false);
			for (int i = 0; i < this._bufferList.Count; i++)
				file.WriteLine(this._bufferList[i].Text);
			file.Flush();
			file.Close();
		}

		public CString Join(string pSep)
		{
			CString retVal = new CString();
			for (int i = 0; i < this._bufferList.Count - 1; i++)
				retVal += new CString() + pSep + this._bufferList[i];
			if (this._bufferList.Count != null)
				retVal += this._bufferList[this._bufferList.Count - 1];
			return retVal;
		}
		#endregion

		#region	Get-Value Functions
		public int Count
		{
			get
			{
				return this._bufferList.Count;
			}
		}
		#endregion
	}
}
