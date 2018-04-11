using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SemVer
{
	/// <summary>
	///   Implementation of Semantic Verisoning standard, version 2.0.0.
	///   See https://semver.org/ for specifications.
	/// </summary>
	public class SemVer : IComparable<SemVer>, IEquatable<SemVer>
	{
		public int Major { get; }
		public int Minor { get; }
		public int Patch { get; }
		
		public string[] PreReleaseIdentifiers { get; }
		public string[] BuildMetadataIdentifiers { get; }
		
		public string PreRelease => string.Join(".", PreReleaseIdentifiers);
		public string BuildMetadata => string.Join(".", BuildMetadataIdentifiers);
		
		
		public SemVer(int major, int minor, int patch)
			: this(major, minor, patch, new string[0], null,
			                            new string[0], null) {  }
		
		public SemVer(int major, int minor, int patch,
		              string preRelease = "", string buildMetadata = "")
			: this(major, minor, patch,
			       SplitIdentifiers(preRelease), nameof(preRelease),
			       SplitIdentifiers(buildMetadata), nameof(buildMetadata)) {  }
		
		public SemVer(int major, int minor, int patch,
		              string[] preReleaseIdentifiers = null,
		              string[] buildMetadataIdentifiers = null)
			: this(major, minor, patch,
			       preReleaseIdentifiers, nameof(preReleaseIdentifiers),
			       buildMetadataIdentifiers, nameof(buildMetadataIdentifiers)) {  }
		
		private SemVer(int major, int minor, int patch,
		               string[] preReleaseIdentifiers, string preReleaseParamName,
		               string[] buildMetadataIdentifiers, string buildMetadataParamName)
		{
			if (major < 0) throw new ArgumentOutOfRangeException(
				nameof(major), major, "Major value must be 0 or positive");
			if (minor < 0) throw new ArgumentOutOfRangeException(
				nameof(minor), minor, "Minor value must be 0 or positive");
			if (patch < 0) throw new ArgumentOutOfRangeException(
				nameof(patch), patch, "Patch value must be 0 or positive");
			
			if (preReleaseIdentifiers == null)
				preReleaseIdentifiers = new string[0];
			for (var i = 0; i < preReleaseIdentifiers.Length; i++) {
				var ident = preReleaseIdentifiers[i];
				if (ident == null) throw new ArgumentException(
					$"{ preReleaseParamName } contains null element at index { i }", preReleaseParamName);
				if (ident.Length == 0) throw new ArgumentException(
					$"{ preReleaseParamName } contains empty identifier at index { i }", preReleaseParamName);
				if (!IsValidIdentifier(ident)) throw new ArgumentException(
					$"{ preReleaseParamName } contains invalid identifier ('{ ident }') at index { i }", preReleaseParamName);
				if (IsNumericIdent(ident) && (ident[0] == '0') && (ident.Length > 1)) throw new ArgumentException(
					$"{ preReleaseParamName } contains numeric identifier with leading zero(es) at index { i }", preReleaseParamName);
			}
			
			if (buildMetadataIdentifiers == null)
				buildMetadataIdentifiers = new string[0];
			for (var i = 0; i < buildMetadataIdentifiers.Length; i++) {
				var ident = buildMetadataIdentifiers[i];
				if (ident == null) throw new ArgumentException(
					$"{ buildMetadataParamName } contains null element at index { i }", buildMetadataParamName);
				if (ident.Length == 0) throw new ArgumentException(
					$"{ buildMetadataParamName } contains empty identifier at index { i }", buildMetadataParamName);
				if (!IsValidIdentifier(ident)) throw new ArgumentException(
					$"{ buildMetadataParamName } contains invalid identifier ('{ ident }') at index { i }", buildMetadataParamName);
			}
			
			Major = major;
			Minor = minor;
			Patch = patch;
			
			PreReleaseIdentifiers    = preReleaseIdentifiers;
			BuildMetadataIdentifiers = buildMetadataIdentifiers;
		}
		
		
		/// <summary>
		///   Converts the specified string representation of a
		///   semantic version to its <see cref="SemVer"/> equivalent.
		/// </summary>
		/// <exception cref="ArgumentNullException"> Thrown if the specified string is null. </exception>
		/// <exception cref="FormatException"> Thrown if the specified string doesn't contain a proper properly formatted semantic version. </exception>
		public static SemVer Parse(string s)
		{
			TryParse(s, out var result, true);
			return result;
		}
		
		/// <summary>
		///   Tries to convert the specified string representation of a
		///   semantic version to its <see cref="SemVer"/> equivalent,
		///   returning true if successful.
		///   
		///   Regardless of success, the result parameter will contain
		///   a valid, non-null SemVer with the method's best guess.
		/// </summary>
		/// <exception cref="ArgumentNullException"> Thrown if the specified string is null. </exception>
		public static bool TryParse(string s, out SemVer result)
			=> TryParse(s, out result, false);
		
		private static readonly string[] PART_LOOKUP = {
			"MAJOR", "MINOR", "PATCH", "PRE_RELEASE", "BUILD_METADATA" };
		private static bool TryParse(string s, out SemVer result, bool throwException)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));
			var sb    = new StringBuilder();
			var error = false;
			
			var mode = 0;
			var versions = new int[3];
			var data     = new []{ new List<string>(), new List<string>() };
			
			var i = 0;
			
			void ThrowOrSetError(string message, params object[] args)
			{
				if (throwException) throw new FormatException(
					$"Error parsing version string '{ s }' at index { i }: "
					+ string.Format(message, args.Select(element => {
						// If a char? argument is passed, treat it in a special way.
						if (element is char?) {
							var chr = (char?)element;
							return (chr != null) ? $"'{ chr }'" : "end of string";
						} else return element;
					}).ToArray()));
				else error = true;
			}
			
			for (; i <= s.Length; i++) {
				var chr = (i < s.Length) ? s[i] : (char?)null;
				if (mode <= 2) {
					if ((chr >= '0') && (chr <= '9')) {
						sb.Append(chr);
						if ((sb.Length == 2) && (sb[0] == '0'))
							ThrowOrSetError("{0} version contains leading zero", PART_LOOKUP[mode]);
					} else {
						if (sb.Length == 0)
							ThrowOrSetError("Expected {0} version, found {1}", PART_LOOKUP[mode], chr);
						else versions[mode] = int.Parse(sb.ToString());
						sb.Clear();
						
						if (chr == '.') mode++;
						else {
							if (mode != 2)
								ThrowOrSetError("Expected dot and {0} version, found {1}", PART_LOOKUP[mode + 1], chr);
							if (chr == '-') mode = 3;
							else if (chr == '+') mode = 4;
							else if (chr != null) { mode = 3; i--; }
						}
					}
				} else if (mode <= 4) {
					if (chr.HasValue && IsValidIdentifierChar((char)chr)) sb.Append(chr);
					else if (!((chr == '+') && (mode == 3)) && (chr != '.') && (chr != null))
						ThrowOrSetError("Unexpected character {0} in {1} identifier", chr, PART_LOOKUP[mode]);
					else {
						if (sb.Length == 0)
							ThrowOrSetError("Expected {0} identifier, found {1}", PART_LOOKUP[mode], chr);
						else {
							var ident = sb.ToString();
							if ((mode == 3) && IsNumericIdent(ident) && (ident[0] == '0') && (ident.Length > 1)) {
								ThrowOrSetError("{ 0 } numeric identifier contains leading zero", PART_LOOKUP[mode]);
								ident = ident.TrimStart('0');
							}
							data[mode - 3].Add(ident);
						}
						sb.Clear();
						if ((chr == '+') && (mode == 3)) mode++;
					}
				}
			}
			
			result = new SemVer(versions[0], versions[1], versions[2],
			                    data[0].ToArray(), data[1].ToArray());
			return !error;
		}
		
		
		public override string ToString()
		{
			var sb = new StringBuilder()
				.Append(Major).Append('.')
				.Append(Minor).Append('.')
				.Append(Patch);
			if (PreReleaseIdentifiers.Length > 0)
				sb.Append('-').Append(PreRelease);
			if (BuildMetadataIdentifiers.Length > 0)
				sb.Append('+').Append(BuildMetadata);
			return sb.ToString();
		}
		
		
		public static bool operator ==(SemVer left, SemVer right)
			=> ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
		public static bool operator !=(SemVer left, SemVer right)
			=> !(left == right);
		
		// NOTE: The relational operators behave like lifted Nullable<T> operators:
		//       If either operand, or BOTH operands are null, they return false.
		//       If you want to compare the order of SemVer instances including
		//       null as a valid value, use the Compare or CompareTo methods.
		
		public static bool operator >(SemVer left, SemVer right)
			=> (left != null) && (right != null) && (Compare(left, right) > 0);
		public static bool operator <(SemVer left, SemVer right)
			=> (left != null) && (right != null) && (Compare(left, right) < 0);
		public static bool operator >=(SemVer left, SemVer right)
			=> (left != null) && (right != null) && (Compare(left, right) >= 0);
		public static bool operator <=(SemVer left, SemVer right)
			=> (left != null) && (right != null) && (Compare(left, right) <= 0);
		
		
		public int CompareTo(SemVer other)
			=> Compare(this, other);
		public static int Compare(SemVer left, SemVer right)
		{
			if (ReferenceEquals(left, right)) return 0;
			if (ReferenceEquals(left, null)) return -1;
			if (ReferenceEquals(right, null)) return 1;
			
			var majorDiff = left.Major.CompareTo(right.Major);
			if (majorDiff != 0) return majorDiff;
			var minorDiff = left.Minor.CompareTo(right.Minor);
			if (minorDiff != 0) return minorDiff;
			var patchDiff = left.Patch.CompareTo(right.Patch);
			if (patchDiff != 0) return patchDiff;
			
			var minCount = Math.Min(left.PreReleaseIdentifiers.Length,
			                        right.PreReleaseIdentifiers.Length);
			for (var i = 0; i < minCount; i++) {
				var leftIndent  = left.PreReleaseIdentifiers[i];
				var rightIndent = right.PreReleaseIdentifiers[i];
				var leftIdentIsNumeric  = IsNumericIdent(leftIndent);
				var rightIdentIsNumeric = IsNumericIdent(rightIndent);
				
				// If the ident type is different (one is numeric, the other isn't), sort by
				// which one is the numeric one. Numeric identifiers have lower precedence.
				if (leftIdentIsNumeric != rightIdentIsNumeric)
					return (leftIdentIsNumeric) ? -1 : 1;
				
				var identDiff = (leftIdentIsNumeric)
					// If they're numeric, compare them as numbers.
					? int.Parse(leftIndent).CompareTo(int.Parse(rightIndent))
					// Otherwise compare them lexically in ASCII sort order.
					: string.Compare(leftIndent, rightIndent, StringComparison.Ordinal);
				
				// Only return the difference if there is one,
				// otherwise move on to the next identifier.
				if (identDiff != 0) return identDiff;
			}
			
			// When reaching this point, either the amount of identifiers
			// differ between the two versions, or they're truly equivalent.
			return left.PreReleaseIdentifiers.Length - right.PreReleaseIdentifiers.Length;
		}
		
		public bool Equals(SemVer other)
			=> (other != null) &&
			   (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch) &&
			   (PreRelease == other.PreRelease) && (BuildMetadata == other.BuildMetadata);
		
		public override bool Equals(object obj)
			=> Equals(obj as SemVer);
		
		public override int GetHashCode()
			=> (Major << 24) ^ (Minor << 16) ^ (Patch << 8) ^
			   (PreRelease.GetHashCode() << 4) ^ BuildMetadata.GetHashCode();
		
		
		// Various private helper methods...
		
		/// <summary>
		///   Returns whether the specified string contains only valid
		///   identifier characters. That is, only alphanumeric characters
		///   and hyphens, [0-9A-Za-z-]. Does not check for empty identifiers.
		/// </summary>
		private static bool IsValidIdentifier(string ident)
		{
			for (var i = 0; i < ident.Length; i++)
				if (!IsValidIdentifierChar(ident[i]))
					return false;
			return true;
		}
		
		private static bool IsValidIdentifierChar(char chr)
			=> ((chr >= '0') && (chr <= '9')) ||
				((chr >= 'A') && (chr <= 'Z')) ||
				((chr >= 'a') && (chr <= 'z')) ||
				(chr == '-');
		
		/// <summary>
		///   Returns whether the specified string is a
		///   numeric identifier (only contains digits).
		/// </summary>
		private static bool IsNumericIdent(string ident)
		{
			for (var i = 0; i < ident.Length; i++)
				if ((ident[i] < '0') || (ident[i] > '9'))
					return false;
			return true;
		}
		
		/// <summary>
		///   Splits a string into dot-separated identifiers.
		///   Both null and empty strings return an empty array.
		/// </summary>
		private static string[] SplitIdentifiers(string str)
			=> !string.IsNullOrEmpty(str) ? str.Split('.') : new string[0];
	}
}
