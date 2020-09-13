// https://github.com/lewa-j/Unity-Source-Tools/blob/master/Assets/Code/Read/VTFLoader.cs

using UnityEngine;
using System;
using System.IO;

namespace VTFFile
{
    public class VTFLoader
    {
        private struct VTFHeader
        {
            public string signature;        // File signature ("VTF\0"). (or as little-endian integer, 0x00465456)
            public uint[] version;          //2	// version[0].version[1] (currently 7.2).
            public uint headerSize;         // Size of the header struct (16 byte aligned; currently 80 bytes).
            public short width;             // Width of the largest mipmap in pixels. Must be a power of 2.
            public short height;            // Height of the largest mipmap in pixels. Must be a power of 2.
            public uint flags;              // VTF flags.
            public ushort frames;           // Number of frames, if animated (1 for no animation).
            public ushort firstFrame;       // First frame in animation (0 based).
            public byte[] padding0;         //4	// reflectivity padding (16 byte alignment).
            public Vector3 reflectivity;    //3	// reflectivity vector.
            public byte[] padding1;         //4	// reflectivity padding (8 byte packing).
            public float bumpmapScale;      // Bumpmap scale.
            public VTFImageFormat highResImageFormat;   // High resolution image format.
            public byte mipmapCount;        // Number of mipmaps.
            public VTFImageFormat lowResImageFormat;    // Low resolution image format (always DXT1).
            public byte lowResImageWidth;   // Low resolution image width.
            public byte lowResImageHeight;  // Low resolution image height.
            public ushort depth;            // Depth of the largest mipmap in pixels.
                                            // Must be a power of 2. Can be 0 or 1 for a 2D texture (v7.2 only).
        }

        public enum VTFImageFormat
        {
            IMAGE_FORMAT_RGBA8888 = 0,              //!<  = Red, Green, Blue, Alpha - 32 bpp
            IMAGE_FORMAT_ABGR8888,                  //!<  = Alpha, Blue, Green, Red - 32 bpp
            IMAGE_FORMAT_RGB888,                    //!<  = Red, Green, Blue - 24 bpp
            IMAGE_FORMAT_BGR888,                    //!<  = Blue, Green, Red - 24 bpp
            IMAGE_FORMAT_RGB565,                    //!<  = Red, Green, Blue - 16 bpp
            IMAGE_FORMAT_I8,                        //!<  = Luminance - 8 bpp
            IMAGE_FORMAT_IA88,                      //!<  = Luminance, Alpha - 16 bpp
            IMAGE_FORMAT_P8,                        //!<  = Paletted - 8 bpp
            IMAGE_FORMAT_A8,                        //!<  = Alpha- 8 bpp
            IMAGE_FORMAT_RGB888_BLUESCREEN,         //!<  = Red, Green, Blue, "BlueScreen" Alpha - 24 bpp
            IMAGE_FORMAT_BGR888_BLUESCREEN,         //!<  = Red, Green, Blue, "BlueScreen" Alpha - 24 bpp
            IMAGE_FORMAT_ARGB8888,                  //!<  = Alpha, Red, Green, Blue - 32 bpp
            IMAGE_FORMAT_BGRA8888,                  //!<  = Blue, Green, Red, Alpha - 32 bpp
            IMAGE_FORMAT_DXT1,                      //!<  = DXT1 compressed format - 4 bpp
            IMAGE_FORMAT_DXT3,                      //!<  = DXT3 compressed format - 8 bpp
            IMAGE_FORMAT_DXT5,                      //!<  = DXT5 compressed format - 8 bpp
            IMAGE_FORMAT_BGRX8888,                  //!<  = Blue, Green, Red, Unused - 32 bpp
            IMAGE_FORMAT_BGR565,                    //!<  = Blue, Green, Red - 16 bpp
            IMAGE_FORMAT_BGRX5551,                  //!<  = Blue, Green, Red, Unused - 16 bpp
            IMAGE_FORMAT_BGRA4444,                  //!<  = Red, Green, Blue, Alpha - 16 bpp
            IMAGE_FORMAT_DXT1_ONEBITALPHA,          //!<  = DXT1 compressed format with 1-bit alpha - 4 bpp
            IMAGE_FORMAT_BGRA5551,                  //!<  = Blue, Green, Red, Alpha - 16 bpp
            IMAGE_FORMAT_UV88,                      //!<  = 2 channel format for DuDv/Normal maps - 16 bpp
            IMAGE_FORMAT_UVWQ8888,                  //!<  = 4 channel format for DuDv/Normal maps - 32 bpp
            IMAGE_FORMAT_RGBA16161616F,             //!<  = Red, Green, Blue, Alpha - 64 bpp
            IMAGE_FORMAT_RGBA16161616,              //!<  = Red, Green, Blue, Alpha signed with mantissa - 64 bpp
            IMAGE_FORMAT_UVLX8888,                  //!<  = 4 channel format for DuDv/Normal maps - 32 bpp
            IMAGE_FORMAT_R32F,                      //!<  = Luminance - 32 bpp
            IMAGE_FORMAT_RGB323232F,                //!<  = Red, Green, Blue - 96 bpp
            IMAGE_FORMAT_RGBA32323232F,             //!<  = Red, Green, Blue, Alpha - 128 bpp
            IMAGE_FORMAT_NV_DST16,
            IMAGE_FORMAT_NV_DST24,
            IMAGE_FORMAT_NV_INTZ,
            IMAGE_FORMAT_NV_RAWZ,
            IMAGE_FORMAT_ATI_DST16,
            IMAGE_FORMAT_ATI_DST24,
            IMAGE_FORMAT_NV_NULL,
            IMAGE_FORMAT_ATI2N,
            IMAGE_FORMAT_ATI1N,

            IMAGE_FORMAT_COUNT,
            IMAGE_FORMAT_NONE = -1
        }

        public static Texture2D LoadFile(string name, string path)
        {
            BinaryReader BR = new BinaryReader(File.Open(path, FileMode.Open));

            VTFHeader header = ReadHeader(BR);

            ReadData(
                BR,
                header,
                 out byte[] ImageData,
                 out uint ImageDataSize,
                 out byte[] ThumbnailImageData,
                 out uint ThumbnailDataSize
             );

            BR.BaseStream.Dispose();

            return CreateTexture(name, header, ImageData);
        }

        private static VTFHeader ReadHeader(BinaryReader BR)
        {
            _ = BR.BaseStream.Seek(0, SeekOrigin.Begin);
            return new VTFHeader
            {
                signature = new string(BR.ReadChars(4)),
                version = new uint[] { BR.ReadUInt32(), BR.ReadUInt32() },
                headerSize = BR.ReadUInt32(),
                width = (short)BR.ReadUInt16(),
                height = (short)BR.ReadUInt16(),
                flags = BR.ReadUInt32(),
                frames = BR.ReadUInt16(),
                firstFrame = BR.ReadUInt16(),
                padding0 = BR.ReadBytes(4),
                reflectivity = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle()),
                padding1 = BR.ReadBytes(4),
                bumpmapScale = BR.ReadSingle(),
                highResImageFormat = (VTFImageFormat)BR.ReadUInt32(),
                mipmapCount = BR.ReadByte(),
                lowResImageFormat = (VTFImageFormat)BR.ReadUInt32(),
                lowResImageWidth = BR.ReadByte(),
                lowResImageHeight = BR.ReadByte(),
                depth = BR.ReadUInt16()
            };
        }

        private static void ReadData(BinaryReader BR, VTFHeader header, out byte[] data, out uint dataSize, out byte[] ThumbData, out uint thumbSize)
        {
            dataSize = ComputeImageSize(header.width, header.height, header.mipmapCount, header.frames, header.highResImageFormat);

            thumbSize = ComputeImageSize(header.lowResImageWidth, header.lowResImageHeight, header.lowResImageFormat);

            uint ThumbnailDataOffset = 0;
            uint ImageDataOffset = 0;

            ThumbnailDataOffset = header.headerSize;
            ImageDataOffset = ThumbnailDataOffset + thumbSize;

            BR.BaseStream.Seek(ThumbnailDataOffset, SeekOrigin.Begin);
            ThumbData = BR.ReadBytes((int)thumbSize);

            BR.BaseStream.Seek(ImageDataOffset, SeekOrigin.Begin);
            data = BR.ReadBytes((int)dataSize);
        }

        private static Texture2D CreateTexture(string name, VTFHeader header, byte[] ImageData)
        {
            Texture2D tex;
            if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_DXT5)
            {
                tex = new Texture2D((int)header.width, (int)header.height, TextureFormat.DXT5, false);
                int offset = (header.width * header.height);
                byte[] buf = new byte[offset];
                Buffer.BlockCopy(ImageData, ImageData.Length - offset, buf, 0, offset);

                tex.LoadRawTextureData(buf);
                tex.Apply(true);
                Color32[] tempCol = tex.GetPixels32();
                tex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, true);
                tex.SetPixels32(tempCol);
                tex.Apply(true);
                tex.Compress(true);
                tex.name = name;

                return tex;
            }
            else if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1)
            {
                tex = new Texture2D((int)header.width, (int)header.height, TextureFormat.DXT1, false);
                int offset = ((header.width * header.height) / 2) * header.frames;
                byte[] buf = new byte[offset];
                Buffer.BlockCopy(ImageData, ImageData.Length - offset, buf, 0, offset);
                tex.LoadRawTextureData(buf);
                tex.Apply();

                Color32[] tempCol = tex.GetPixels32();
                tex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, true);
                tex.SetPixels32(tempCol);
                tex.Apply(true);
                tex.Compress(true);
                tex.name = name;
                return tex;
            }
            else if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGR888)
            {
                tex = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGB24, true);
                Color32[] colors = new Color32[header.width * header.height];
                for (int i = 0; i < header.width * header.height; i++)
                {
                    colors[i] = new Color32(ImageData[i * 3 + 2], ImageData[i * 3 + 1], ImageData[i * 3], 255);
                }
                tex.SetPixels32(colors);
                tex.Apply();
                tex.name = name;
                return tex;
            }
            else if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGRA8888)
            {
                tex = new Texture2D((int)header.width, (int)header.height, TextureFormat.BGRA32, false);
                int imageSize = header.width * header.height * 4;
                byte[] buf = new byte[imageSize];
                Buffer.BlockCopy(ImageData, ImageData.Length - imageSize, buf, 0, imageSize);
                tex.LoadRawTextureData(buf);
                tex.Apply();
                tex.name = name;
                return tex;
            }
            else
            {
                Debug.LogWarning(name + " Unsuported Texture Format" + header.highResImageFormat);
                return null;
            }
        }

        private static uint ComputeImageSize(int Width, int Height, VTFImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case VTFImageFormat.IMAGE_FORMAT_DXT1:
                case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                    if (Width < 4 && Width > 0)
                        Width = 4;

                    if (Height < 4 && Height > 0)
                        Height = 4;

                    return (((uint)Width + 3) / 4) * (((uint)Height + 3) / 4) * 8;
                //return (uint)Width * (uint)Height * 1;
                case VTFImageFormat.IMAGE_FORMAT_DXT3:
                case VTFImageFormat.IMAGE_FORMAT_DXT5:
                    if (Width < 4 && Width > 0)
                        Width = 4;

                    if (Height < 4 && Height > 0)
                        Height = 4;

                    return (((uint)Width + 3) / 4) * (((uint)Height + 3) / 4) * 16;
                case VTFImageFormat.IMAGE_FORMAT_ABGR8888:
                case VTFImageFormat.IMAGE_FORMAT_ARGB8888:
                case VTFImageFormat.IMAGE_FORMAT_RGBA8888:
                case VTFImageFormat.IMAGE_FORMAT_BGRA8888:
                case VTFImageFormat.IMAGE_FORMAT_BGRX8888:
                case VTFImageFormat.IMAGE_FORMAT_UVWQ8888:
                case VTFImageFormat.IMAGE_FORMAT_UVLX8888:
                case VTFImageFormat.IMAGE_FORMAT_R32F:
                case VTFImageFormat.IMAGE_FORMAT_NV_INTZ:
                case VTFImageFormat.IMAGE_FORMAT_NV_RAWZ:
                case VTFImageFormat.IMAGE_FORMAT_NV_NULL:
                    return (uint)Width * (uint)Height * 4;
                case VTFImageFormat.IMAGE_FORMAT_RGB888:
                case VTFImageFormat.IMAGE_FORMAT_BGR888:
                case VTFImageFormat.IMAGE_FORMAT_RGB888_BLUESCREEN:
                case VTFImageFormat.IMAGE_FORMAT_BGR888_BLUESCREEN:
                case VTFImageFormat.IMAGE_FORMAT_NV_DST24:
                    return (uint)Width * (uint)Height * 3;
                case VTFImageFormat.IMAGE_FORMAT_RGB565:
                case VTFImageFormat.IMAGE_FORMAT_IA88:
                case VTFImageFormat.IMAGE_FORMAT_BGR565:
                case VTFImageFormat.IMAGE_FORMAT_BGRX5551:
                case VTFImageFormat.IMAGE_FORMAT_BGRA4444:
                case VTFImageFormat.IMAGE_FORMAT_BGRA5551:
                case VTFImageFormat.IMAGE_FORMAT_UV88:
                case VTFImageFormat.IMAGE_FORMAT_NV_DST16:
                case VTFImageFormat.IMAGE_FORMAT_ATI_DST16:
                    return (uint)Width * (uint)Height * 2;
                default:
                    return (uint)Width * (uint)Height * 1;//GetImageFormatInfo(imageFormat).BytesPerPixel;
            }
        }

        private static uint ComputeImageSize(int width, int height, int mipmaps, uint frames, VTFImageFormat imageFormat)
        {
            uint imageSize = 0;

            for (uint i = 0; i < mipmaps; i++)
            {
                imageSize += ComputeImageSize(width, height, imageFormat);

                width >>= 1;
                height >>= 1;

                if (width < 1)
                    width = 1;

                if (height < 1)
                    height = 1;
            }
            imageSize *= frames;
            return imageSize;
        }
    }
}
