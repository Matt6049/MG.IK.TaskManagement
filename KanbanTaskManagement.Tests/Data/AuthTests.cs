using KanbanTaskManagement.Data;

namespace KanbanTaskManagement.Tests.Data;

public class AuthTests {
	[Fact]
	public void CreateCredential_ThenVerify_WithCorrectPassword_Succeeds() {
		var (hash, salt) = Auth.CreateCredential("correct-password");

		Assert.True(Auth.Verify("correct-password", hash, salt));
	}

	[Fact]
	public void Verify_WithWrongPassword_Fails() {
		var (hash, salt) = Auth.CreateCredential("correct-password");

		Assert.False(Auth.Verify("wrong-password", hash, salt));
	}

	[Fact]
	public void Verify_IsCaseSensitive() {
		var (hash, salt) = Auth.CreateCredential("Password1");

		Assert.False(Auth.Verify("password1", hash, salt));
	}

	[Fact]
	public void Verify_WithMalformedHash_ReturnsFalseInsteadOfThrowing() {
		var salt = Auth.GenerateSalt();

		Assert.False(Auth.Verify("anything", "not-base64!!", salt));
	}

	[Fact]
	public void Verify_WithMalformedSalt_ReturnsFalseInsteadOfThrowing() {
		var (hash, _) = Auth.CreateCredential("anything");

		Assert.False(Auth.Verify("anything", hash, "not-base64!!"));
	}

	[Fact]
	public void GenerateSalt_ProducesDifferentValuesEachTime() {
		var salt1 = Auth.GenerateSalt();
		var salt2 = Auth.GenerateSalt();

		Assert.NotEqual(salt1, salt2);
	}

	[Fact]
	public void CreateCredential_SamePasswordTwice_ProducesDifferentHashes() {
		var (hash1, _) = Auth.CreateCredential("same-password");
		var (hash2, _) = Auth.CreateCredential("same-password");

		Assert.NotEqual(hash1, hash2);
	}

	[Fact]
	public void GetHash_IsDeterministicForSamePasswordAndSalt() {
		var salt = Auth.GenerateSalt();

		Assert.Equal(Auth.GetHash("password", salt), Auth.GetHash("password", salt));
	}
}
