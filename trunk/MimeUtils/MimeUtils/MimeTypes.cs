#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2008 Stephen M. McKamey

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

using System;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Drawing.Imaging;

namespace MimeUtils
{
	/// <summary>
	/// http://www.iana.org/assignments/media-types/
	/// </summary>
	public static class MimeTypes
	{
		#region Constants

		public const string AppSettingsKey_MimeMapXml = "MimeMapXml";
		private static readonly Dictionary<string, MimeType> MimeByExtension;
		private static readonly Dictionary<string, MimeType> MimeByContentType;
		public static readonly MimeType[] ConfigTypes;

		#endregion Constants

		#region Init

		static MimeTypes()
		{
			string mimeMapXml = System.Configuration.ConfigurationManager.AppSettings[MimeTypes.AppSettingsKey_MimeMapXml];
			if (!String.IsNullOrEmpty(mimeMapXml))
			{
				mimeMapXml = System.Web.Hosting.HostingEnvironment.MapPath(mimeMapXml);

				if (String.IsNullOrEmpty(mimeMapXml))
				{
					// if not a website, could load as relative from DLL:
					string binFolder = System.Reflection.Assembly.GetExecutingAssembly().Location;
					binFolder = Path.GetDirectoryName(binFolder);
					mimeMapXml = Path.Combine(binFolder, mimeMapXml);
				}
			}

			try
			{
				if (File.Exists(mimeMapXml))
				{
					using (FileStream stream = File.OpenRead(mimeMapXml))
					{
						System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(MimeType[]));
						MimeTypes.ConfigTypes = serializer.Deserialize(stream) as MimeType[];
					}

					////Sort and serialize back to XML
					//using (FileStream stream = File.OpenWrite(mimeMapXml+"_RoundTrip.xml"))
					//{
					//    stream.SetLength(0);

					//    ////Generate sample data
					//    //MimeTypes.ConfigTypes = new MimeType[2];
					//    //MimeTypes.ConfigTypes[0] = new MimeType();
					//    //MimeTypes.ConfigTypes[0].FileExts = new string[1];
					//    //MimeTypes.ConfigTypes[0].FileExts[0] = ".xml";
					//    //MimeTypes.ConfigTypes[0].ContentTypes = new string[2];
					//    //MimeTypes.ConfigTypes[0].ContentTypes[0] = "application/xml";
					//    //MimeTypes.ConfigTypes[0].ContentTypes[1] = "text/xml";
					//    //MimeTypes.ConfigTypes[1] = new MimeType();
					//    //MimeTypes.ConfigTypes[1].FileExts = new string[2];
					//    //MimeTypes.ConfigTypes[1].FileExts[0] = ".jpg";
					//    //MimeTypes.ConfigTypes[1].FileExts[1] = ".jpeg";
					//    //MimeTypes.ConfigTypes[1].ContentTypes = new string[1];
					//    //MimeTypes.ConfigTypes[1].ContentTypes[0] = "image/jpeg";

					//    Array.Sort(MimeTypes.ConfigTypes);
					//    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(MimeType[]));
					//    serializer.Serialize(stream, MimeTypes.ConfigTypes);
					//}
				}
				else
				{
					MimeTypes.ConfigTypes = new MimeType[0];
				}

				MimeTypes.MimeByExtension = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Length);
				MimeTypes.MimeByContentType = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Length);

				foreach (MimeType mime in MimeTypes.ConfigTypes)
				{
					try
					{
						foreach (string fileExt in mime.FileExts)
						{
							string key = fileExt.ToLowerInvariant();
							if (mime.Primary || !MimeTypes.MimeByExtension.ContainsKey(key))
								MimeTypes.MimeByExtension[key] = mime;
						}

						foreach (string contentType in mime.ContentTypes)
						{
							string key = contentType.ToLowerInvariant();
							if (mime.Primary || !MimeTypes.MimeByContentType.ContainsKey(key))
								MimeTypes.MimeByContentType[key] = mime;
						}
					}
					catch { }
				}
			}
			catch
			{
				MimeTypes.ConfigTypes = new MimeType[0];
				MimeTypes.MimeByExtension = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Length);
				MimeTypes.MimeByContentType = new Dictionary<string, MimeType>(MimeTypes.ConfigTypes.Length);
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
				return null;
			}

			extension = extension.ToLowerInvariant();
			if (!MimeTypes.MimeByExtension.ContainsKey(extension))
			{
				return null;
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
				return null;
			}

			contentType = contentType.ToLowerInvariant();
			if (!MimeTypes.MimeByContentType.ContainsKey(contentType))
			{
				return null;
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
		/// Gets ImageFormat by extension.
		/// </summary>
		/// <param name="extension">file extension (e.g. ".png")</param>
		/// <returns>ImageFormat</returns>
		public static ImageFormat GetImageFormat(string extension)
		{
			if (String.IsNullOrEmpty(extension))
			{
				return null;
			}

			// use MimeTypes collection to standardize the extension
			MimeType mime = MimeTypes.GetByExtension(extension);
			if (mime != null)
			{
				extension = mime.FileExt.ToLowerInvariant();
			}
			else
			{
				extension = extension.ToLowerInvariant();
			}

			switch (extension.TrimStart('.'))
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

		#endregion Extension Methods
	}

	/// <summary>
	/// Multipurpose Internet Mail Extensions (MIME) type
	/// </summary>
	[Serializable]
	public class MimeType : IComparable
	{
		#region Fields

		private string name = null;
		private string description = null;
		private string[] fileExts = null;
		private string[] contentTypes = null;
		private MimeCategory category = MimeCategory.Unknown;
		private bool primary = false;

		#endregion Fields

		#region Properties

		[DefaultValue("")]
		[XmlElement("Name")]
		public string Name
		{
			get
			{
				if (this.name == null)
				{
					return String.Empty;
				}
				return this.name;
			}
			set { this.name = value; }
		}

		[DefaultValue("")]
		[XmlElement("Description")]
		public string Description
		{
			get
			{
				if (this.description == null)
				{
					return String.Empty;
				}
				return this.description;
			}
			set { this.description = value; }
		}

		[DefaultValue(null)]
		[XmlElement("FileExt")]
		public string[] FileExts
		{
			get { return this.fileExts; }
			set
			{
				if (value == null)
				{
					value = new string[0];
				}

				foreach (string fileExt in value)
				{
					if (fileExt == null || fileExt.IndexOf('.') < 0)
					{
						throw new FormatException("FileExt is not correct format: "+fileExt);
					}
				}

				this.fileExts = value;
			}
		}

		[DefaultValue(null)]
		[XmlElement("ContentType")]
		public string[] ContentTypes
		{
			get { return this.contentTypes; }
			set
			{
				if (value == null)
				{
					value = new string[0];
				}

				foreach (string contentType in value)
				{
					if (contentType == null || contentType.IndexOf('/') < 0)
					{
						throw new FormatException("ContentType is not correct format: "+contentType);
					}
				}

				this.contentTypes = value;
			}
		}

		[DefaultValue(MimeCategory.Unknown)]
		[XmlElement("Category")]
		public MimeCategory Category
		{
			get { return this.category; }
			set { this.category = value; }
		}

		[Description("Gets and sets this as the primary type when resolution is ambiguous.")]
		[DefaultValue(false)]
		[XmlAttribute("primary")]
		public bool Primary
		{
			get { return this.primary; }
			set { this.primary = value; }
		}

		/// <summary>
		/// Gets the dominant content type for this MIME type.
		/// </summary>
		[XmlIgnore]
		public string ContentType
		{
			get
			{
				if (this.ContentTypes == null || this.ContentTypes.Length < 1)
				{
					return String.Empty;
				}
				return this.ContentTypes[0];
			}
		}

		/// <summary>
		/// Gets the dominant file extension for this MIME type.
		/// </summary>
		[XmlIgnore]
		public string FileExt
		{
			get
			{
				if (this.FileExts == null || this.FileExts.Length < 1)
				{
					return String.Empty;
				}
				return this.FileExts[0];
			}
		}

		#endregion Properties

		#region IComparable Members

		int IComparable.CompareTo(object obj)
		{
			MimeType that = obj as MimeType;
			if (that == null)
			{
				return 1;
			}

			if (this.FileExts.Length == 0 &&
				that.FileExts.Length == 0)
			{
				return this.Name.CompareTo(that.Name);
			}

			return this.FileExts[0].CompareTo(that.FileExts[0]);
		}

		#endregion
	}

	/// <summary>
	/// Large categories for file types.
	/// </summary>
	public enum MimeCategory
	{
		Unknown,
		Folder,
		Audio,
		Binary,
		Code,
		Compressed,
		Document,
		Image,
		Text,
		Video,
		Xml
	}
}
