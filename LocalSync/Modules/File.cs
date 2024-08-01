using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalSync.Modules
{


    public class File : DataType
    {
        public string file_name { get; set; }
        private static long file_ID = 0;
        private long this_file_ID;
        private string file_path;
        private string file_type;
        private long file_size;
        private DateTime date_modified;
        public string icon_name;
        public string Name => file_name;
        public string dataType => "file";
        public string dataFileIcon => icon_name;

        public static readonly List<string> supported_executable_files = new List<string>()
        {
            ".exe",
            ".msi",
            ".app",
            ".deb",
            ".dmg",
            ".rpm",
            ".bin", 
            ".pkg", 
        };

        public static readonly List<string> supported_script_files = new List<string>()
        {
            ".psi",
            ".bat",
            ".sh",
            ".bin",
            ".pl", 
        };

        public static readonly List<string> supported_coding_files = new List<string>()
        {
            ".py",
            ".jar",
            ".cs",
            ".r",
            ".js",
            ".cpp",
            ".vbs",
            ".go",
        };

        public static readonly List<string> supported_rich_text_files = new List<string>()
        {
            ".rtf",
            ".txt",
            ".odt",
            ".html",
            ".htm",
            ".mht",
            ".md",
            ".mhtml",
        };

        public static readonly List<string> supported_compressed_files = new List<string>()
        {
            ".zip",
            ".rar",
            ".7z",
            ".tar.gz",
            ".tgz",
            ".arj",
            ".sitx",
            ".bz2",
            ".cab",
            ".lzh",
        };

        public static readonly List<string> supported_img_files = new List<string>()
        {
            ".jpeg",
            ".jpg",
            ".tiff",
            ".tif",
            ".raw",
            ".bmp",
            ".gif",
            ".png",
            ".webp",
            ".heic",
            ".psd",
            ".avif",
        };

        public File(string file_name, DateTime date_modified, string file_type, long file_size, string file_path) 
        {
            this.file_name = file_name;
            this.file_path = file_path;
            this.file_type = file_type;
            this.file_size = file_size;
            this.this_file_ID = file_ID;
            this.date_modified = date_modified;
            this.icon_name = this.match_file_icon(file_type);

            file_ID += 1; 
        }

        internal string match_file_icon(string file_type)
        {
            switch (file_type){
                case string when (supported_executable_files.Contains(file_type)):
                    return "\uED35";

                case string when (supported_rich_text_files.Contains(file_type)):
                    return "\uE8A5";

                case string when (supported_coding_files.Contains(file_type)):
                    return "\uE943"; 

                case string when (supported_compressed_files.Contains(file_type)):
                    return "\uF012";

                case string when (supported_script_files.Contains(file_type)):
                    return "\uE756";

                case string when (supported_img_files.Contains(file_type)):
                    return "\uE722";

                default:
                    return "\uE729"; 
            }
        }

        //internal FontIcon create_icon(string glyph_code)
        //{
        //    FontIcon icon = new FontIcon();
        //    icon.Glyph = glyph_code;
        //    return icon;
        //}

        public string GetFileName() { return file_name; }
        public string GetFilePath() { return file_path; }
    }
}
