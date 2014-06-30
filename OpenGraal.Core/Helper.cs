using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGraal.Core
{
	class Helper
	{
/*
string read_line(std::ifstream& stream) {
  string line;
  std::getline(stream, line);
  return Graal::strip(line, "\r\n");
}
  
string strip(string str, const char* ws) {
  string::size_type first, last, length;
  first = str.find_first_not_of(ws);
  last = str.find_last_not_of(ws);
  
  if (first == string::npos)
    first = 0;

  length = last - first + 1;

  return str.substr(first, length);
}
*/
		static string BASE64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    public int get_tile_x(int index) {
      return index % 16 + index/512 * 16;
    }

    public int get_tile_y(int index) {
      return index / 16 - index/512 * 32;
    }

	public int get_tile_index(int x, int y)
	{
      return x / 16 * 512 + x % 16 + y * 16;
    }

int parse_base64(string[] str)
{
	int num = 0;
	for (int i = 0; i < str.Length; ++i)
	{
		int pos = BASE64.IndexOf(str[i]);
		/*
		if (pos == string.N) {
			throw new Exception("BASE64: invalid format");
		}
		*/
		num += (pos) << (str.Length - i - 1) * 6;
	}
	return num;
}

string format_base64(int num, int len) {
  string str;
  for (int i = 0; i < len; ++i) {
    int index = (num >> (len - i - 1) * 6) & 0x3F; // 6 bit per character
    str += BASE64[index];
  }
  return str;
}

	}
}
