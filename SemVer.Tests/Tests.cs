using System;
using Xunit;

namespace SemVer.Tests
{
	public class Tests
	{
		[Fact]
		public void Constructor_Exceptions()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new SemVer(-1, 0, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new SemVer(0, -1, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new SemVer(0, 0, -1));
			
			Assert.Throws<ArgumentException>(() => new SemVer(0, 0, 0, new string[]{ null }));
			Assert.Throws<ArgumentException>(() => new SemVer(0, 0, 0, null, new string[]{ null }));
		}
		
		[Theory]
		[InlineData("*")]
		[InlineData(".")]
		[InlineData("..")]
		[InlineData("0..0")]
		[InlineData("0.")]
		[InlineData(".0")]
		[InlineData("01")]
		public void Constructor_Invalid_PreRelease(string preRelease)
		{
			Assert.Throws<ArgumentException>(() => new SemVer(0, 0, 0, preRelease));
		}
		
		[Theory]
		[InlineData("*")]
		[InlineData(".")]
		[InlineData("..")]
		[InlineData("0..0")]
		[InlineData("0.")]
		[InlineData(".0")]
		public void Constructor_Invalid_BuildMetadata(string buildMetadata)
		{
			Assert.Throws<ArgumentException>(() => new SemVer(0, 0, 0, null, buildMetadata));
		}
		
		[Theory]
		[InlineData("0.0.0")]
		[InlineData("23.6.107")]
		[InlineData("1.2.3-4.5.6+7.8.9")]
		[InlineData("1.0.0-rc-5.1")]
		[InlineData("1.0.0+001de7b")]
		// As I understand from reading the specification, hyphens can't just be part
		// of identifiers, but there's also nothing disallowing this from being valid.
		[InlineData("1.0.0--.-.-b+-p.99-4--")]
		public void Parse_ToString(string str)
		{
			Assert.Equal(str, SemVer.Parse(str).ToString());
		}
		
		[Theory]
		[InlineData("", "0.0.0")]
		[InlineData(" 10", "0.0.0-10")]
		[InlineData("14", "14.0.0")]
		[InlineData("1.2.3.4.5-6", "1.2.3-4.5-6")]
		[InlineData("1.2.3-+4", "1.2.3+4")]
		[InlineData("14.6.2pre3", "14.6.2-pre3")]
		[InlineData("14.6beta9", "14.6.0-beta9")]
		[InlineData("bloob", "0.0.0-bloob")]
		[InlineData("-+x", "0.0.0+x")]
		[InlineData("10+ab+CC", "10.0.0+abCC")]
		[InlineData("Hello There. I'm new!", "0.0.0-HelloThere.Imnew")]
		[InlineData("004.02.8--beta008...-..007+00010", "4.2.8--beta008.-.7+00010")]
		public void TryParse_ToString_BestGuess(string str, string guess)
		{
			var success = SemVer.TryParse(str, out var version);
			Assert.False(success);
			Assert.Equal(guess, version.ToString());
		}
	}
}
