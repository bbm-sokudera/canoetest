using System;

public class ImageUtilsCn
{
    /*
     * 削除またはコメントアウトするメソッド
    public static void ConvertDepthToColorData(byte[] depthData, float depthScale, ref byte[] colorData)
    {
        // ... (省略) ...
    }
    */

    public static void Convert8BitIrToByteArray(byte[] irData, ref byte[] colorData)
    {
        int colorDataLength = irData.Length * 3;
        if (colorData == null || colorData.Length != colorDataLength)
        {
            colorData = new byte[colorDataLength];
        }

        for (int i = 0; i < irData.Length; i++)
        {
            byte irValue = irData[i];
            int index = i * 3;
            colorData[index] = irValue; // Red
            colorData[index + 1] = irValue; // Green
            colorData[index + 2] = irValue; // Blue
        }
    }

    public static void Convert16BitIrToColorData(byte[] irData, ref byte[] colorData)
    {
        int colorDataLength = (irData.Length / 2) * 3;
        if (colorData == null || colorData.Length != colorDataLength)
        {
            colorData = new byte[colorDataLength];
        }

        for (int i = 0; i < irData.Length; i += 2)
        {
            ushort irValue = (ushort)(irData[i + 1] << 8 | irData[i]);
            byte irByte = (byte)(irValue >> 8); // Scale down to 8 bits

            int index = (i / 2) * 3;
            colorData[index] = irByte; // Red
            colorData[index + 1] = irByte; // Green
            colorData[index + 2] = irByte; // Blue
        }
    }
}