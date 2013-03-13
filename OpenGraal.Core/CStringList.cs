using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGraal.Core
{
	public class CStringList : IEnumerable
	{
		/// <summary>
		/// Player-List
		/// </summary>
		//internal Framework Server;
		public Dictionary<int, CString> BufferList = new Dictionary<int, CString>();

		protected Int32 Position = -1;
		protected int id = 0;
		/*
		~CStringList()
		{
			for(int i = 0; i < count(); i++)
				delete (CString)CList::item(i);
		}
		*/
		public int add(string pStr)
		{
			CString newString;
			if(pStr == null)
				newString = new CString();
			else
			{
				newString = new CString();
				newString.Write(pStr);
			}
			BufferList[this.id] = newString;
			int listId = this.id;
			this.id++;
			return listId;
		}

		public int add(CString pString)
		{
			CString newString = new CString();
			newString.Write(pString);
			BufferList[this.id] = newString;
			int listId = this.id;
			this.id++;
			return listId;
		}

		public void remove(int pIndex)
		{
			CString str = (CString)BufferList[pIndex];
			if(str == null)
				return;
			//str.Remove();
			BufferList.Remove(pIndex);
		}

		public void clear()
		{
			if(BufferList.Count <= 0)
				return;
			/*
			for(int i = 0; i < count(); i++)
			{
				CString str = (CString)CList::item(i);
				delete str;
			}
			*/

			BufferList.Clear();
		}

		public CString item(int pIndex)
		{
			return (CString)BufferList[pIndex];
		}

		public void replace(int pIndex, string pString)
		{
			CString str = (CString)BufferList[pIndex];
			if (str == null)
				return;
			//delete str;
			
			str = new CString();
			str.Write(pString);
			BufferList[pIndex] = str;
		}

		public bool load(string pFileName)
		{
			string[] lines = System.IO.File.ReadAllLines(@""+pFileName);
			this.clear();
			//FILE* file = fopen(pFileName, "r");
			//string file = System.IO.File.ReadAllText(@""+pFileName);
			if(lines.Length == 0)
				return false;
			//Read each line and add it to list
			foreach(string line in lines)
			{
				//Trim end of line (spaces allowed)
				/*
				for(int i = (int)line.Length-1; i >= 0; i--)
				{
					if(line.Length < 1)
						line = "\0";
					else break;
				}
				*/
				this.add(line.TrimEnd());
			}
			//fclose(file);
			return true;
		}

		public void load(string pInput, char pSep)
		{
			
			CString newString = new CString();
			//this.clear();
			newString.Write(pInput);
			
			string[] lines = newString.Text.Split(pSep);
			foreach(string line in lines)
			{
				this.add(line);
				//line = strtok(null, pSep);
			}
		}

		public void load(string[] pStrings, int pCount)
		{
			this.clear();
			for(int i = 0; i < pCount; i++)
				add(pStrings[i]);
		}

		public int find(CString pString)
		{
			for(int i = 0; i < this.BufferList.Count; i++)
			{
				if(this.BufferList[i] == pString)
					return i;
			}
			return -1;
		}
		/*
		int findI(const char* pString)
		{
			for(int i = 0; i < count(); i++)
			{
				if(stricmp(item(i).text(), pString) == 0)
					return i;
			}
			return -1;
		}
		*/
		/*
		void save(const char* pFileName)
		{
			FILE* file = fopen(pFileName, "wb");
			for(int i = 0; i < count(); i++)
				fprintf(file, "%s\r\n", item(i).text());
			fclose(file);
		}
		*/
		/*
		CString join(char pSep)
		{
			CString retVal;
			for(int i = 0; i < this.BufferList.Count-1; i++)
				retVal += pSep + this.BufferList[i];
			if (this.BufferList.Count != null)
				retVal += this.BufferList[this.BufferList.Count - 1];
			return retVal;
		}*/
	}
}
