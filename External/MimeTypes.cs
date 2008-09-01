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
using System.Web.Hosting;

using MediaLib.Web.Hosting;

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
		public static readonly MimeType ImageJpeg;
		public static readonly MimeType ImageGif;
		public static readonly MimeType ImagePng;
		public static readonly MimeType JavaScript;
		public static readonly MimeType CssStyleSheet;
		public static readonly MimeType Html;
		public static readonly MimeType Xml;
		public static readonly MimeType Rss;

		internal const string JavaScriptContentType = "application/javascript";
		internal const string CssStyleSheetContentType = "text/css";

		#endregion Constants

		#region Init

		static MimeTypes()
		{
			string mimeMapXml = System.Configuration.ConfigurationManager.AppSettings[MimeTypes.AppSettingsKey_MimeMapXml];
			if (!String.IsNullOrEmpty(mimeMapXml))
			{
				mimeMapXml = HostingEnvironment.MapPath(mimeMapXml);
			}

			try
			{
				if (FilePathMapper.FileExists(mimeMapXml))
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
			finally
			{
				MimeTypes.ImageJpeg = MimeTypes.GetByExtension(".jpg");
				if (MimeTypes.ImageJpeg == null)
				{
					MimeTypes.ImageJpeg = new MimeType();
					MimeTypes.ImageJpeg.Name = "JPEG Image";
					MimeTypes.ImageJpeg.FileExts = new String[] { ".jpg", ".jpeg" };
					MimeTypes.ImageJpeg.ContentTypes = new String[] { "image/jpeg", "image/pjpeg" };
					MimeTypes.ImageJpeg.Category = MimeCategory.Image;
					MimeTypes.ImageJpeg.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.ImageJpeg.FileExts[0]] = MimeTypes.ImageJpeg;
					MimeTypes.MimeByContentType[MimeTypes.ImageJpeg.ContentTypes[0]] = MimeTypes.ImageJpeg;
				}

				MimeTypes.ImageGif = MimeTypes.GetByExtension(".gif");
				if (MimeTypes.ImageGif == null)
				{
					MimeTypes.ImageGif = new MimeType();
					MimeTypes.ImageGif.Name = "GIF Image";
					MimeTypes.ImageGif.FileExts = new String[] { ".gif", ".gif" };
					MimeTypes.ImageGif.ContentTypes = new String[] { "image/gif" };
					MimeTypes.ImageGif.Category = MimeCategory.Image;
					MimeTypes.ImageGif.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.ImageGif.FileExts[0]] = MimeTypes.ImageGif;
					MimeTypes.MimeByContentType[MimeTypes.ImageGif.ContentTypes[0]] = MimeTypes.ImageGif;
				}

				MimeTypes.ImagePng = MimeTypes.GetByExtension(".png");
				if (MimeTypes.ImagePng == null)
				{
					MimeTypes.ImagePng = new MimeType();
					MimeTypes.ImagePng.Name = "PNG Image";
					MimeTypes.ImagePng.FileExts = new String[] { ".png" };
					MimeTypes.ImagePng.ContentTypes = new String[] { "image/png" };
					MimeTypes.ImagePng.Category = MimeCategory.Image;
					MimeTypes.ImagePng.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.ImagePng.FileExts[0]] = MimeTypes.ImagePng;
					MimeTypes.MimeByContentType[MimeTypes.ImagePng.ContentTypes[0]] = MimeTypes.ImagePng;
				}

				MimeTypes.JavaScript = MimeTypes.GetByExtension(".js");
				if (MimeTypes.JavaScript == null)
				{
					MimeTypes.JavaScript = new MimeType();
					MimeTypes.JavaScript.Name = "JavaScript";
					MimeTypes.JavaScript.FileExts = new String[] { ".js" };
					MimeTypes.JavaScript.ContentTypes = new String[] { MimeTypes.JavaScriptContentType, "application/ecmascript", "text/javascript", "text/ecmascript" };
					MimeTypes.JavaScript.Category = MimeCategory.Code;
					MimeTypes.JavaScript.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.JavaScript.FileExts[0]] = MimeTypes.JavaScript;
					MimeTypes.MimeByContentType[MimeTypes.JavaScript.ContentTypes[0]] = MimeTypes.JavaScript;
				}

				MimeTypes.CssStyleSheet = MimeTypes.GetByExtension(".css");
				if (MimeTypes.CssStyleSheet == null)
				{
					MimeTypes.CssStyleSheet = new MimeType();
					MimeTypes.CssStyleSheet.Name = "Cascading StyleSheet";
					MimeTypes.CssStyleSheet.FileExts = new String[] { ".css" };
					MimeTypes.CssStyleSheet.ContentTypes = new String[] { MimeTypes.CssStyleSheetContentType };
					MimeTypes.CssStyleSheet.Category = MimeCategory.Web;
					MimeTypes.CssStyleSheet.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.CssStyleSheet.FileExts[0]] = MimeTypes.CssStyleSheet;
					MimeTypes.MimeByContentType[MimeTypes.CssStyleSheet.ContentTypes[0]] = MimeTypes.CssStyleSheet;
				}

				MimeTypes.Xml = MimeTypes.GetByExtension(".xml");
				if (MimeTypes.Xml == null)
				{
					MimeTypes.Xml = new MimeType();
					MimeTypes.Xml.Name = "XML";
					MimeTypes.Xml.FileExts = new String[] { ".xml" };
					MimeTypes.Xml.ContentTypes = new String[] { "application/xml", "text/xml" };
					MimeTypes.Xml.Category = MimeCategory.Xml;
					MimeTypes.Xml.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.Xml.FileExts[0]] = MimeTypes.Xml;
					MimeTypes.MimeByContentType[MimeTypes.Xml.ContentTypes[0]] = MimeTypes.Xml;
				}

				MimeTypes.Rss = MimeTypes.GetByExtension(".rss");
				if (MimeTypes.Rss == null)
				{
					MimeTypes.Rss = new MimeType();
					MimeTypes.Rss.Name = "RSS";
					MimeTypes.Rss.FileExts = new String[] { ".rss" };
					MimeTypes.Rss.ContentTypes = new String[] { "application/rss+xml" };
					MimeTypes.Rss.Category = MimeCategory.Xml;
					MimeTypes.Rss.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.Rss.FileExts[0]] = MimeTypes.Rss;
					MimeTypes.MimeByContentType[MimeTypes.Rss.ContentTypes[0]] = MimeTypes.Rss;
				}

				MimeTypes.Html = MimeTypes.GetByExtension(".htm");
				if (MimeTypes.Html == null)
				{
					MimeTypes.Html = new MimeType();
					MimeTypes.Html.Name = "HTML";
					MimeTypes.Html.FileExts = new String[] { ".htm", ".html", ".xhtm", ".xhtml" };
					MimeTypes.Html.ContentTypes = new String[] { "text/html", "application/xhtml+xml" };
					MimeTypes.Html.Category = MimeCategory.Web;
					MimeTypes.Html.Primary = true;
					MimeTypes.MimeByExtension[MimeTypes.Html.FileExts[0]] = MimeTypes.Html;
					MimeTypes.MimeByContentType[MimeTypes.Html.ContentTypes[0]] = MimeTypes.Html;
				}
			}
		}

		#endregion Init

		#region Extension Methods

		public static MimeType GetByExtension(string extension)
		{
			if (String.IsNullOrEmpty(extension))
				return null;

			extension = extension.ToLowerInvariant();
			if (!MimeTypes.MimeByExtension.ContainsKey(extension))
				return null;

			return MimeTypes.MimeByExtension[extension];
		}

		public static MimeType GetByContentType(string contentType)
		{
			if (String.IsNullOrEmpty(contentType))
				return null;

			contentType = contentType.ToLowerInvariant();
			if (!MimeTypes.MimeByContentType.ContainsKey(contentType))
				return null;

			return MimeTypes.MimeByContentType[contentType];
		}

		/// <summary>
		/// </summary>
		/// <param name="info"></param>
		/// <returns>MIME Type string</returns>
		/// <remarks>
		/// http://www.webmaster-toolkit.com/mime-types.shtml
		/// http://filext.com/detaillist.php?extdetail=xxx
		/// </remarks>
		public static string GetContentType(string extension)
		{
			MimeType mime = MimeTypes.GetByExtension(extension);
			if (mime == null)
				return String.Empty;

			return mime.ContentType;
		}

		public static MimeCategory GetCategory(string extension)
		{
			if (String.IsNullOrEmpty(extension))
				return MimeCategory.Folder;

			MimeType mime = MimeTypes.GetByExtension(extension);
			if (mime == null)
				return MimeCategory.Unknown;

			return mime.Category;
		}

		public static System.Drawing.Imaging.ImageFormat GetImageFormat(string extension)
		{
			if (String.IsNullOrEmpty(extension))
				return null;

			// use MimeTypes collection to standardize the extension
			MimeType mime = MimeTypes.GetByExtension(extension);
			if (mime != null)
				extension = mime.FileExt.ToLowerInvariant();
			else
				extension = extension.ToLowerInvariant();

			switch (extension.TrimStart('.'))
			{
				case "bmp":
				{
					return System.Drawing.Imaging.ImageFormat.Bmp;
				}
				case "emf":
				{
					return System.Drawing.Imaging.ImageFormat.Emf;
				}
				case "exif":
				{
					return System.Drawing.Imaging.ImageFormat.Exif;
				}
				case "gif":
				{
					return System.Drawing.Imaging.ImageFormat.Gif;
				}
				case "jpg":
				case "jpeg":
				{
					return System.Drawing.Imaging.ImageFormat.Jpeg;
				}
				case "png":
				{
					return System.Drawing.Imaging.ImageFormat.Png;
				}
				case "tif":
				case "tiff":
				{
					return System.Drawing.Imaging.ImageFormat.Tiff;
				}
				case "wmf":
				{
					return System.Drawing.Imaging.ImageFormat.Wmf;
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

	public enum MimeCategory
	{
		Unknown,
		Folder,
		Document,
		Image,
		Audio,
		Video,
		Xml,
		Web,
		Code,
		Binary,
		Compressed
	}
}
