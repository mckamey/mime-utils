#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2009 Stephen M. McKamey

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.

\*---------------------------------------------------------------------------------*/
#endregion License

//#define SEED_CONFIG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
using System.Xml.Serialization;

namespace MimeUtils
{
	/// <summary>
	/// http://www.iana.org/assignments/media-types/
	/// </summary>
	public static class MimeTypes
	{
		#region Constants

		public const string AppSettingsKey_MimeMapXml = "MimeMapXml";
		private const string MimeTypeFilename = "MimeTypes.xml";
		private static readonly Dictionary<string, MimeType> MimeByExtension;
		private static readonly Dictionary<string, MimeType> MimeByContentType;
		public static readonly IList<MimeType> ConfigTypes;

		#endregion Constants

		#region Init

		/// <summary>
		/// CCtor
		/// </summary>
		static MimeTypes()
		{
			string mimeMapXml = ConfigurationManager.AppSettings[MimeTypes.AppSettingsKey_MimeMapXml];
			if (!String.IsNullOrEmpty(mimeMapXml))
			{
				mimeMapXml = HostingEnvironment.MapPath(mimeMapXml);
			}

			if (String.IsNullOrEmpty(mimeMapXml))
			{
				// if not a website, could load as relative from DLL:
				string binFolder = Assembly.GetExecutingAssembly().Location;
				binFolder = Path.GetDirectoryName(binFolder);
				mimeMapXml = Path.Combine(binFolder, MimeTypeFilename);
			}

			if (!File.Exists(mimeMapXml))
			{
#if SEED_CONFIG
				// can use this to seed a file
				// Sort and serialize back to XML
				using (FileStream stream = File.OpenWrite(mimeMapXml+"_RoundTrip.xml"))
				{
					//Generate sample data
					List<MimeType> configTypes = new List<MimeType>(2);
					MimeType mimeType = new MimeType();
					mimeType.FileExts = new string[]
						{
							".xml"
						};
					mimeType.ContentTypes = new string[]
						{
							"application/xml",
							"text/xml"
						};
					configTypes.Add(mimeType);

					mimeType = new MimeType();
					mimeType.FileExts = new string[]
						{
							".jpg",
							".jpeg"
						};
					mimeType.ContentTypes = new string[]
						{
							"image/jpeg"
						};
					configTypes.Add(mimeType);

					stream.SetLength(0);
					XmlSerializer serializer = new XmlSerializer(typeof(MimeType[]));
					serializer.Serialize(stream, MimeTypes.ConfigTypes);

					configTypes.Sort();
					MimeTypes.ConfigTypes = configTypes.AsReadOnly();
				}
#else
				throw new ApplicationException("Could not find mime type XML file at '" + mimeMapXml + "'.");
#endif
			}

			try
			{
				using (FileStream stream = File.OpenRead(mimeMapXml))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(MimeType[]));
					MimeType[] configTypes = serializer.Deserialize(stream) as MimeType[];
					MimeTypes.ConfigTypes = new List<MimeType>(configTypes).AsReadOnly();
				}

				MimeTypes.MimeByExtension = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Count);
				MimeTypes.MimeByContentType = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Count);

				foreach (MimeType mime in MimeTypes.ConfigTypes)
				{
					try
					{
						foreach (string fileExt in mime.FileExts)
						{
							string key = '.'+fileExt.ToLowerInvariant().TrimStart('.');
							if (mime.Primary || !MimeTypes.MimeByExtension.ContainsKey(key))
							{
								MimeTypes.MimeByExtension[key] = mime;
							}
						}

						foreach (string contentType in mime.ContentTypes)
						{
							string key = contentType.ToLowerInvariant();
							if (mime.Primary || !MimeTypes.MimeByContentType.ContainsKey(key))
							{
								MimeTypes.MimeByContentType[key] = mime;
							}
						}
					}
					catch { }
				}
			}
			catch
			{
			    MimeTypes.ConfigTypes = new List<MimeType>().AsReadOnly();
			    MimeTypes.MimeByExtension = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Count);
			    MimeTypes.MimeByContentType = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Count);
			}
		}

		#endregion Init

		#region Extension Methods

		/// <summary>
		/// Gets type information by extension.
		/// </summary>
		/// <param name="extension">file extension (e.g. ".xml")</param>
		/// <returns></returns>
		public static MimeType GetByExtension(string extension)
		{
			if (String.IsNullOrEmpty(extension))
			{
				return MimeType.Empty;
			}

			extension = '.'+extension.ToLowerInvariant().TrimStart('.');
			if (!MimeTypes.MimeByExtension.ContainsKey(extension))
			{
				return MimeType.Empty;
			}

			return MimeTypes.MimeByExtension[extension];
		}

		/// <summary>
		/// Gets type information by MIME content-type.
		/// </summary>
		/// <param name="contentType">content-type (e.g. "text/plain")</param>
		/// <returns></returns>
		public static MimeType GetByContentType(string contentType)
		{
			if (String.IsNullOrEmpty(contentType))
			{
				return MimeType.Empty;
			}

			contentType = contentType.ToLowerInvariant();
			if (!MimeTypes.MimeByContentType.ContainsKey(contentType))
			{
				return MimeType.Empty;
			}

			return MimeTypes.MimeByContentType[contentType];
		}

		/// <summary>
		/// Gets content-type by extension.
		/// </summary>
		/// <param name="extension">file extension (e.g. ".xml")</param>
		/// <returns>MIME content-type</returns>
		/// <remarks>
		/// http://www.webmaster-toolkit.com/mime-types.shtml
		/// http://filext.com/detaillist.php?extdetail=xxx
		/// </remarks>
		public static string GetContentType(string extension)
		{
			MimeType mime = MimeTypes.GetByExtension(extension);
			if (mime == null)
			{
				return String.Empty;
			}

			return mime.ContentType;
		}

		/// <summary>
		/// Gets MIME Category by extension.
		/// </summary>
		/// <param name="extension">file extension (e.g. ".xml")</param>
		/// <returns>MIME Category</returns>
		public static MimeCategory GetCategory(string extension)
		{
			if (String.IsNullOrEmpty(extension))
			{
				return MimeCategory.Folder;
			}

			MimeType mime = MimeTypes.GetByExtension(extension);
			if (mime == null)
			{
				return MimeCategory.Unknown;
			}

			return mime.Category;
		}

		/// <summary>
		/// Gets ImageFormat for a MimeType
		/// </summary>
		/// <param name="mimeType">MimeType</param>
		/// <returns>ImageFormat</returns>
		public static ImageFormat GetImageFormat(MimeType mimeType)
		{
			if (mimeType == null || String.IsNullOrEmpty(mimeType.FileExt))
			{
				return null;
			}

			switch (mimeType.FileExt.TrimStart('.').ToLowerInvariant())
			{
				case "bmp":
				{
					return ImageFormat.Bmp;
				}
				case "emf":
				{
					return ImageFormat.Emf;
				}
				case "exif":
				{
					return ImageFormat.Exif;
				}
				case "gif":
				{
					return ImageFormat.Gif;
				}
				case "jpg":
				case "jpeg":
				{
					return ImageFormat.Jpeg;
				}
				case "png":
				{
					return ImageFormat.Png;
				}
				case "tif":
				case "tiff":
				{
					return ImageFormat.Tiff;
				}
				case "wmf":
				{
					return ImageFormat.Wmf;
				}
				default:
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Gets the image encoder for the given MimeType
		/// </summary>
		/// <param name="mimeType"></param>
		/// <returns>encoder</returns>
		public static ImageCodecInfo GetImageDecoder(MimeType mimeType)
		{
			if (mimeType == null || String.IsNullOrEmpty(mimeType.ContentType))
			{
				return null;
			}

			string contentType = mimeType.ContentType;
			foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(codec.MimeType, contentType))
				{
					return codec;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the image encoder for the given MimeType
		/// </summary>
		/// <param name="mimeType"></param>
		/// <returns>encoder</returns>
		public static ImageCodecInfo GetImageEncoder(MimeType mimeType)
		{
			if (mimeType == null || String.IsNullOrEmpty(mimeType.ContentType))
			{
				return null;
			}

			string contentType = mimeType.ContentType;
			foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(codec.MimeType, contentType))
				{
					return codec;
				}
			}

			return null;
		}

		#endregion Extension Methods
	}
}
