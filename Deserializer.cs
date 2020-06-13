using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

		public static async Task<IEnumerable<byte>> DeserializeCustomizationOptionsAsync(StorageFile customsFile) {
			var stream = await customsFile.OpenAsync(FileAccessMode.Read);

			CustomizationOptions customizationOptions;
			var options = new JsonSerializerOptions {
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			using (var inputStream = stream.AsStreamForRead()) {
				customizationOptions = await JsonSerializer.DeserializeAsync<CustomizationOptions>(inputStream, options);
			}

			if (customizationOptions == null) {
				Debug.WriteLine("Root is null");
				return null;
			}

			String concatenatedCustomizations = "";
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

			return HexStringToByteArray(concatenatedCustomizations);
		}

		private static IEnumerable<byte> HexStringToByteArray(string hex) {
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16));
		}

	}
}
