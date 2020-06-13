using Force.Crc32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class AppLogic {
	public static byte[] GenerateUnlockFileContent(UInt32 steam32ID, IEnumerable<byte> uuidStorage) {
		IEnumerable<byte> unlockVersion = UInt32ToByteArray(0x00000001);
		IEnumerable<byte> crcInput = UInt32ToByteArray(CalculateSaveFileHash(steam32ID, uuidStorage));
		IEnumerable<byte> itemCountBytes = UInt32ToByteArray((UInt32) uuidStorage.Count() / 16);
		return unlockVersion.Concat(crcInput).Concat(itemCountBytes).Concat(uuidStorage).ToArray();
	}

	public static UInt32 Steam64IDtoReversedSteam32(String steam64ID) {
		UInt32 res = (UInt32) UInt64.Parse(steam64ID);
		return ReverseBytes(res);
	}

	private static UInt32 ReverseBytes(UInt32 val) {
		byte[] intAsBytes = BitConverter.GetBytes(val);
		Array.Reverse(intAsBytes);
		return BitConverter.ToUInt32(intAsBytes, 0);
	}

	private static IEnumerable<byte> UInt32ToByteArray(UInt32 integer) {
		return Enumerable.Range(0, 4)
						 .Select(x => (byte) (integer >> 8 * (3 - x)));
	}

	private static UInt32 CalculateSaveFileHash(UInt32 steam32ID, IEnumerable<byte> uuidStorage) {
		IEnumerable<byte> steamID32byteArray = UInt32ToByteArray(steam32ID);
		IEnumerable<byte> appendix = UInt32ToByteArray(0x01001001);
		IEnumerable<byte> crcInput = steamID32byteArray.Concat(appendix).Concat(uuidStorage);
		return Crc32Algorithm.Compute(crcInput.ToArray());
	}
}
