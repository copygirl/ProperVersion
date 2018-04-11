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
		
		[Fact]
		public void Parse_Methods_ThrowOnNull()
		{
			Assert.Throws<ArgumentNullException>(() => SemVer.Parse(null));
			Assert.Throws<ArgumentNullException>(() => SemVer.Parse(null));
			Assert.Throws<ArgumentNullException>(() => SemVer.Parse(null));
		}
		
		[Theory]
		[InlineData("",            "0.0.0",        0, "Expected MAJOR version number, found end of string")]
		[InlineData(" 10",         "0.0.0-10",     0, "Expected MAJOR version number, found ' '")]
		[InlineData("14",          "14.0.0",       2, "Expected MINOR version, found end of string")]
		[InlineData("1.2.3.4.5-6", "1.2.3-4.5-6",  5, "Expected PRE_RELEASE or BUILD_METADATA, found '.'")]
		[InlineData("1.2.3-+4",    "1.2.3+4",      6, "Expected PRE_RELEASE identifier, found '+'")]
		[InlineData("14.6.2pre3",  "14.6.2-pre3",  6, "Expected PRE_RELEASE or BUILD_METADATA, found 'p'")]
		[InlineData("14.6beta9",   "14.6.0-beta9", 4, "Expected PATCH version, found 'b'")]
		[InlineData("bloob",       "0.0.0-bloob",  0, "Expected MAJOR version number, found 'b'")]
		[InlineData("-+x",         "0.0.0+x",      0, "Expected MAJOR version number, found '-'")]
		[InlineData("10+ab+CC",    "10.0.0+abCC",  2, "Expected MINOR version, found '+'")]
		[InlineData("Hello There. I'm new!",
		            "0.0.0-HelloThere.Imnew",
		            0, "Expected MAJOR version number, found 'H'")]
		[InlineData("004.02.8--beta008...-..007+00010",
		            "4.2.8--beta008.-.7+00010",
		            1, "MAJOR version contains leading zero")]
		public void Parse_Invalid_With_Best_Guess(string str, string expectedGuess,
		                                          int expectedIndex, string expectedError)
		{
			Assert.Throws<FormatException>(() => SemVer.Parse(str));
			var success = SemVer.TryParse(str, out var version, out var error);
			Assert.False(success);
			Assert.Equal(expectedGuess, version.ToString());
			Assert.Equal($"Error parsing version string '{ str }' at index { expectedIndex }: { expectedError }", error);
		}
	}
}
