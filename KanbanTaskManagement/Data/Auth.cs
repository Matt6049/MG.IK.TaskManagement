using System.Security.Cryptography;

namespace KanbanTaskManagement.Data;

public static class Auth {
	private const int SaltBytes = 16;
	private const int HashBytes = 32;
	private const int Iterations = 100_000;
	private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA256;

	public static string GenerateSalt()
		=> Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltBytes));

	public static string GetHash(string plaintext, string salt) {
		var saltBytes = Convert.FromBase64String(salt);
		var hash = Rfc2898DeriveBytes.Pbkdf2(plaintext, saltBytes, Iterations, Algo, HashBytes);
		return Convert.ToBase64String(hash);
	}

	public static bool Verify(string password, string hash, string salt) {
		try {
			var expected = Convert.FromBase64String(hash);
			var actual = Convert.FromBase64String(GetHash(password, salt));
			return CryptographicOperations.FixedTimeEquals(expected, actual);
		} catch (FormatException) {
			return false;
		}
	}

	public static (string Hash, string Salt) CreateCredential(string password) {
		var salt = GenerateSalt();
		return (GetHash(password, salt), salt);
	}
}
