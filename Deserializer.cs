using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SMOutfitUnlocker_WPF {
    class Deserializer {
		private class CustomizationOptions {
			public Category[] categoryList { get; set; }
		}

		private class Category {
			public String name { get; set; }
			public Option[] options { get; set; }
		}

		private class Option {
			public String uuid { get; set; }
		}

		public static async Task<(IEnumerable<byte> bytes, string orError)> DeserializeCustomizationOptionsAsync(StorageFile customsFile) {
			if (customsFile == null) return (null, "Customsfile could not be opened (0).");
			var stream = await customsFile.OpenAsync(FileAccessMode.Read);
			CustomizationOptions customizationOptions;

			//Skip comments in JSON file
			var options = new JsonSerializerOptions {
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			if (stream == null) return (null, "Customsfile could not be opened (1).");
			try {
				using (var inputStream = stream.AsStreamForRead()) {
					if (stream == null) return (null, "Customsfile could not be opened (2).");
					customizationOptions = await JsonSerializer.DeserializeAsync<CustomizationOptions>(inputStream, options);
				}
			} catch (Exception) {
				return (null, "Customsfile content not recognized (0).");
			}

			if (customizationOptions == null) {
				Debug.WriteLine("Root is null");
				return (null, "Customsfile content not recognized (1).");
			}

			String concatenatedCustomizations = "";
			try {
				int customizationCount = 0;
				String[] unlockableCustomizationCategories = { "Hats", "Torso", "Gloves", "Legs", "Shoes", "Backpack" };
				foreach (Category cat in customizationOptions.categoryList) {
					if (unlockableCustomizationCategories.Contains(cat.name)) {
						foreach (Option option in cat.options) {
							concatenatedCustomizations += option.uuid.Replace("-", "").ToUpper();
							customizationCount++;
						}
					}
				}
			} catch (Exception) {
				return (null, "Customsfile content not recognized (2).");
			}

			IEnumerable<byte> content = HexStringToByteArray(concatenatedCustomizations); ;
			if (content == null) return (null, "Customsfile content interpretation error");
			return (content, null);
		}

		private static IEnumerable<byte> HexStringToByteArray(string hex) {
			if (hex.Length % 2 == 1) return null;
			if (!Regex.IsMatch(hex, @"\A\b[0-9a-fA-F]+\b\Z")) return null; //invalid character(s) in string
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16));
		}

	}
}
