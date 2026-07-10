using KanbanTaskManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace KanbanTaskManagement.Data;

public static class Auth {
	public static string GenerateSalt() {
		string salt = "";
		Random rand = new Random();
		for (int i = 0; i < 8; i++) {
			salt += Convert.ToChar(rand.Next(0, Int16.MaxValue));
		}
		return salt;
	}

	public static string GetHash(string plaintext, string salt) {
		var bytePass = Encoding.UTF32.GetBytes(salt + plaintext);
		var hash = SHA256.HashData(bytePass);
		return new string(Encoding.UTF32.GetChars(hash));
	}
}
