using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Octopus_File_Vault
{
    /// <summary>
    /// Helper class to categorize files by their type based on extension
    /// </summary>
    public static class FileTypeCategorizer
    {
        // Define file extension sets for each category
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".webp", ".svg", ".ico", ".heic", ".raw"
        };

        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp"
        };

        private static readonly HashSet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".doc", ".docx", ".pdf", ".txt", ".rtf", ".odt", ".xls", ".xlsx", ".ppt", ".pptx",
            ".csv", ".md", ".html", ".htm", ".xml", ".json"
        };

        private static readonly HashSet<string> ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab", ".tgz"
        };

        /// <summary>
        /// Enum representing file categories
        /// </summary>
        public enum FileCategory
        {
            Image,
            Video,
            Document,
            Archive,
            Other
        }

        /// <summary>
        /// Determines the category of a file based on its extension
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The file category</returns>
        public static FileCategory GetFileCategory(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return FileCategory.Other;

            string extension = Path.GetExtension(filePath);

            if (ImageExtensions.Contains(extension))
                return FileCategory.Image;

            if (VideoExtensions.Contains(extension))
                return FileCategory.Video;

            if (DocumentExtensions.Contains(extension))
                return FileCategory.Document;

            if (ArchiveExtensions.Contains(extension))
                return FileCategory.Archive;

            return FileCategory.Other;
        }

        /// <summary>
        /// Checks if the file belongs to the Image category
        /// </summary>
        public static bool IsImage(string filePath) =>
            GetFileCategory(filePath) == FileCategory.Image;

        /// <summary>
        /// Checks if the file belongs to the Video category
        /// </summary>
        public static bool IsVideo(string filePath) =>
            GetFileCategory(filePath) == FileCategory.Video;

        /// <summary>
        /// Checks if the file belongs to the Document category
        /// </summary>
        public static bool IsDocument(string filePath) =>
            GetFileCategory(filePath) == FileCategory.Document;

        /// <summary>
        /// Checks if the file belongs to the Archive category
        /// </summary>
        public static bool IsArchive(string filePath) =>
            GetFileCategory(filePath) == FileCategory.Archive;

        /// <summary>
        /// Gets the appropriate icon for a file based on its category
        /// </summary>
        public static string GetCategoryIcon(string filePath)
        {
            switch (GetFileCategory(filePath))
            {
                case FileCategory.Image:
                    return "🖼️";
                case FileCategory.Video:
                    return "🎬";
                case FileCategory.Document:
                    return "📄";
                case FileCategory.Archive:
                    return "🗂️";
                default:
                    return "📄";
            }
        }
    }
}