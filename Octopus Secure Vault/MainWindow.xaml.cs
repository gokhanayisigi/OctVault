using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json; // Add this for JSON serialization
using System.Text.Json.Serialization; // Add this for JSON attributes

namespace Octopus_File_Vault
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string selectedFilePath;
        private List<EncryptedFileInfo> encryptedFiles = new List<EncryptedFileInfo>();
        private string currentDecryptionKey; // Store the current decryption key
        private EncryptedFileInfo currentSelectedFile; // Store the currently selected file for operations
        private readonly string databaseFilePath; // Path to the store.octus file

        public MainWindow()
        {
            InitializeComponent();
            // Set the database file path to the application's root directory
            databaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "store.octus");

            // Load encrypted files data from the database when application starts
            LoadEncryptedFilesFromDatabase();
        }

        // Method to load encrypted files from the database file
        private void LoadEncryptedFilesFromDatabase()
        {
            try
            {
                // Check if the database file exists
                if (File.Exists(databaseFilePath))
                {
                    // Read the encrypted content from the database file
                    string encryptedContent = File.ReadAllText(databaseFilePath);

                    // If file exists but is empty or contains invalid data, initialize with an empty list
                    if (string.IsNullOrWhiteSpace(encryptedContent))
                    {
                        encryptedFiles = new List<EncryptedFileInfo>();
                        return;
                    }

                    // Deserialize the JSON content to a List<EncryptedFileInfo>
                    encryptedFiles = JsonSerializer.Deserialize<List<EncryptedFileInfo>>(encryptedContent);

                    // Verify files still exist and add them to the UI
                    if (encryptedFiles != null && encryptedFiles.Count > 0)
                    {
                        foreach (var fileInfo in encryptedFiles.ToArray()) // Use ToArray to avoid collection modification during iteration
                        {
                            if (File.Exists(fileInfo.FilePath))
                            {
                                // Add the file to the vault UI
                                AddFileToVault(fileInfo);
                            }
                            else
                            {
                                // Remove files that no longer exist
                                encryptedFiles.Remove(fileInfo);
                            }
                        }

                        // Save the database again in case we removed any files
                        SaveEncryptedFilesToDatabase();

                        // Show the vault and hide empty state if we have files
                        if (encryptedFiles.Count > 0)
                        {
                            EmptyVaultState.Visibility = Visibility.Collapsed;
                            FilesListView.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    // If the file doesn't exist, create an empty one
                    encryptedFiles = new List<EncryptedFileInfo>();
                    SaveEncryptedFilesToDatabase();
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that might occur
                CustomMessageBox.Show(this, $"Error loading encrypted files database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Initialize with an empty list on error
                encryptedFiles = new List<EncryptedFileInfo>();
                SaveEncryptedFilesToDatabase(); // Create a new database file
            }
        }

        // Method to save encrypted files to the database file
        private void SaveEncryptedFilesToDatabase()
        {
            try
            {
                // Serialize the list to JSON
                string jsonContent = JsonSerializer.Serialize(encryptedFiles, new JsonSerializerOptions
                {
                    WriteIndented = true // Make the JSON readable with indentation
                });

                // Write the JSON to the database file
                File.WriteAllText(databaseFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                // Handle any errors that might occur
                CustomMessageBox.Show(this, $"Error saving encrypted files database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseFiles_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void ChangeFile_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a file to encrypt",
                Filter = "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                DisplaySelectedFile(selectedFilePath);
            }
        }

        private void DisplaySelectedFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                // Get file info
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                string fileSize = FormatFileSize(fileInfo.Length);

                // Update UI
                FileNameTextBlock.Text = fileName;
                FileSizeTextBlock.Text = fileSize;

                // Show file selected state and hide drop zone
                FileDropZone.Visibility = Visibility.Collapsed;
                FileSelectedState.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, $"Error accessing file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal size = bytes;

            while (size > 1024 && counter < suffixes.Length - 1)
            {
                size /= 1024;
                counter++;
            }

            return $"{Math.Round(size, 2)} {suffixes[counter]}";
        }

        // Modified EncryptFile_Click to save the database after adding a file
        private void EncryptFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                CustomMessageBox.Show(this, "Please select a valid file to encrypt.", "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generate a random key for AES encryption
                string encryptionKey = GenerateRandomKey(12);
                currentDecryptionKey = encryptionKey; // Store the key for later use

                // Create output filename with .octopus extension
                string fileName = System.IO.Path.GetFileName(selectedFilePath);
                string encryptedFileName = $"{fileName}.octopus";
                string outputPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, encryptedFileName);

                // Encrypt the file
                EncryptFileWithAES(selectedFilePath, outputPath, encryptionKey);

                // Find the TextBlock in the visual tree and set its text
                DecryptionKeyTextBlock.Text = encryptionKey;

                // Show the encryption success dialog
                EncryptionSuccessDialog.Visibility = Visibility.Visible;

                // Add file to the list of encrypted files
                FileInfo fileInfo = new FileInfo(outputPath);
                EncryptedFileInfo encryptedFileInfo = new EncryptedFileInfo
                {
                    FileName = encryptedFileName,
                    FilePath = outputPath,
                    FileSize = fileInfo.Length,
                    EncryptionMethod = "AES 256",
                    EncryptionDate = DateTime.Now,
                    DecryptionKey = encryptionKey
                };

                encryptedFiles.Add(encryptedFileInfo);

                // Update the vault
                AddFileToVault(encryptedFileInfo);

                // Save the updated encrypted files list to the database
                SaveEncryptedFilesToDatabase();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, $"Error encrypting file: {ex.Message}", "Encryption Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateRandomKey(int length)
        {
            // Characters to use for the key
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            // Use cryptographically secure random number generator
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[length];
                rng.GetBytes(data);

                StringBuilder result = new StringBuilder(length);
                foreach (byte b in data)
                {
                    result.Append(chars[b % chars.Length]);
                }

                return result.ToString();
            }
        }

        private void EncryptFileWithAES(string inputFile, string outputFile, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Make sure key is the right length for AES-256 (32 bytes)
            using (SHA256 sha256 = SHA256.Create())
            {
                keyBytes = sha256.ComputeHash(keyBytes);
            }

            // Generate random IV
            byte[] iv = new byte[16]; // AES block size = 16 bytes
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }

            // Create AES encryptor
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                using (FileStream fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    // Write IV to the beginning of the output file
                    fsOutput.Write(iv, 0, iv.Length);

                    // Encrypt input stream and write to output file
                    using (CryptoStream cs = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] buffer = new byte[8192]; // 8K buffer
                        int bytesRead;

                        while ((bytesRead = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cs.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }

        private void AddFileToVault(EncryptedFileInfo fileInfo)
        {
            // Create a new file item in the vault
            Border fileItemBorder = new Border
            {
                Style = (Style)FindResource("FileItemStyle"),
                Tag = fileInfo  // Store the file info with the border for reference
            };

            Grid fileItemGrid = new Grid();
            fileItemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fileItemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fileItemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // File icon
            Border iconBorder = new Border
            {
                Background = (SolidColorBrush)FindResource("SecondaryBackground"),
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 15, 0)
            };

            TextBlock iconText = new TextBlock
            {
                Text = "📄",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);
            fileItemGrid.Children.Add(iconBorder);

            // File details
            StackPanel detailsPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock fileNameText = new TextBlock
            {
                Text = fileInfo.FileName,
                FontFamily = new FontFamily("Garamond"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("PrimaryText")
            };

            detailsPanel.Children.Add(fileNameText);

            // File metadata
            StackPanel metadataPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            TextBlock fileSizeText = new TextBlock
            {
                Text = FormatFileSize(fileInfo.FileSize),
                FontFamily = new FontFamily("Garamond"),
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryText")
            };

            TextBlock bulletPoint1 = new TextBlock
            {
                Text = "•",
                FontFamily = new FontFamily("Garamond"),
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryText"),
                Margin = new Thickness(8, 0, 8, 0)
            };

            TextBlock encMethodText = new TextBlock
            {
                Text = fileInfo.EncryptionMethod,
                FontFamily = new FontFamily("Garamond"),
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryText")
            };

            TextBlock bulletPoint2 = new TextBlock
            {
                Text = "•",
                FontFamily = new FontFamily("Garamond"),
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryText"),
                Margin = new Thickness(8, 0, 8, 0)
            };

            TextBlock dateText = new TextBlock
            {
                Text = fileInfo.EncryptionDate.ToString("dd.MM.yyyy"),
                FontFamily = new FontFamily("Garamond"),
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("SecondaryText")
            };

            metadataPanel.Children.Add(fileSizeText);
            metadataPanel.Children.Add(bulletPoint1);
            metadataPanel.Children.Add(encMethodText);
            metadataPanel.Children.Add(bulletPoint2);
            metadataPanel.Children.Add(dateText);

            detailsPanel.Children.Add(metadataPanel);
            Grid.SetColumn(detailsPanel, 1);
            fileItemGrid.Children.Add(detailsPanel);

            // Menu button
            Button menuButton = new Button
            {
                Content = "⋮",
                FontSize = 18,
                Style = (Style)FindResource("MenuButtonStyle"),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (SolidColorBrush)FindResource("SecondaryText"),
                Tag = fileInfo // Store file info for reference
            };

            menuButton.Click += FileOptionsMenu_Click;

            Grid.SetColumn(menuButton, 2);
            fileItemGrid.Children.Add(menuButton);

            fileItemBorder.Child = fileItemGrid;

            // Add to the panel
            EncryptedFilesPanel.Children.Add(fileItemBorder);

            // Show the vault and hide empty state
            EmptyVaultState.Visibility = Visibility.Collapsed;
            FilesListView.Visibility = Visibility.Visible;
        }

        private void FileOptionsMenu_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag is EncryptedFileInfo fileInfo)
            {
                // Store the currently selected file
                currentSelectedFile = fileInfo;

                // Get the position of the button
                Point position = button.TranslatePoint(new Point(0, 0), this);

                // Position the popup next to the button
                FileOptionsPopup.HorizontalOffset = position.X;
                FileOptionsPopup.VerticalOffset = position.Y;

                // Open the popup
                FileOptionsPopup.IsOpen = true;

                // Set focus to the popup to enable keyboard navigation
                FileOptionsPopup.Focus();
            }
        }

        // Modified ImportEncryptedFile_Click to save the database after importing a file
        private void ImportEncryptedFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select encrypted file to import",
                Filter = "Encrypted files (*.octopus)|*.octopus"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string importedFilePath = openFileDialog.FileName;

                try
                {
                    // Create encrypted file info for the imported file
                    FileInfo fileInfo = new FileInfo(importedFilePath);

                    // Check if file is already in the vault
                    bool fileExists = encryptedFiles.Exists(f => string.Equals(f.FilePath, importedFilePath, StringComparison.OrdinalIgnoreCase));

                    if (fileExists)
                    {
                        CustomMessageBox.Show(this, "This file is already in your vault.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Create file info object
                    EncryptedFileInfo encryptedFileInfo = new EncryptedFileInfo
                    {
                        FileName = fileInfo.Name,
                        FilePath = importedFilePath,
                        FileSize = fileInfo.Length,
                        EncryptionMethod = "AES 256",
                        EncryptionDate = fileInfo.CreationTime,
                        DecryptionKey = "" // Key will be provided by user when decrypting
                    };

                    // Add to collection and UI
                    encryptedFiles.Add(encryptedFileInfo);
                    AddFileToVault(encryptedFileInfo);

                    // Save the updated encrypted files list to the database
                    SaveEncryptedFilesToDatabase();

                    CustomMessageBox.Show(this, "File imported successfully.", "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, $"Error importing file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Method to handle opening file location
        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedFile != null && File.Exists(currentSelectedFile.FilePath))
            {
                // Close the popup
                FileOptionsPopup.IsOpen = false;

                try
                {
                    // Get the directory of the file
                    string directory = Path.GetDirectoryName(currentSelectedFile.FilePath);

                    // Open the file's location in Windows Explorer and select the file
                    Process.Start("explorer.exe", $"/select,\"{currentSelectedFile.FilePath}\"");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, $"Error opening file location: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Modified DeleteFile_Click to save the database after removing a file
        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedFile != null)
            {
                // Close the popup
                FileOptionsPopup.IsOpen = false;

                // Ask for confirmation
                MessageBoxResult result = CustomMessageBox.Show(
                    this,
                    "Are you sure you want to remove this file from your vault?\n\nNote: This will only remove it from the vault, not from your disk.",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove from collection
                    encryptedFiles.Remove(currentSelectedFile);

                    // Remove from UI
                    RemoveFileFromVault(currentSelectedFile);

                    // Save the updated encrypted files list to the database
                    SaveEncryptedFilesToDatabase();

                    CustomMessageBox.Show(this, "File removed from vault.", "File Removed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }


        // Method to remove a file from the vault UI
        private void RemoveFileFromVault(EncryptedFileInfo fileInfo)
        {
            // Find the border containing this file info
            Border borderToRemove = null;

            foreach (UIElement element in EncryptedFilesPanel.Children)
            {
                if (element is Border border && border.Tag is EncryptedFileInfo info)
                {
                    if (info == fileInfo)
                    {
                        borderToRemove = border;
                        break;
                    }
                }
            }

            // Remove the item if found
            if (borderToRemove != null)
            {
                EncryptedFilesPanel.Children.Remove(borderToRemove);

                // Show empty state if no files remain
                if (EncryptedFilesPanel.Children.Count == 0)
                {
                    FilesListView.Visibility = Visibility.Collapsed;
                    EmptyVaultState.Visibility = Visibility.Visible;
                }
            }
        }

        // Implement decryption functionality
        private void DecryptFile_Click(object sender, RoutedEventArgs e)
        {
            // Close the popup
            FileOptionsPopup.IsOpen = false;

            if (currentSelectedFile != null && File.Exists(currentSelectedFile.FilePath))
            {
                // Clear previous input
                DecryptionKeyInput.Text = string.Empty;

                // Show decryption dialog
                DecryptionDialog.Visibility = Visibility.Visible;
            }
            else
            {
                CustomMessageBox.Show(this, "File no longer exists at the specified location.", "Decryption Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Implement decrypt confirmation
        private void ConfirmDecrypt_Click(object sender, RoutedEventArgs e)
        {
            string decryptionKey = DecryptionKeyInput.Text.Trim();

            if (string.IsNullOrEmpty(decryptionKey))
            {
                CustomMessageBox.Show(this, "Please enter a decryption key.", "Decryption Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Hide the dialog
            DecryptionDialog.Visibility = Visibility.Collapsed;

            try
            {
                // Generate output file path
                string encryptedFileName = currentSelectedFile.FileName;
                string originalFileName = Path.GetFileNameWithoutExtension(encryptedFileName); // Remove .octopus

                // If the file is already named .octopus, we need special handling
                if (originalFileName.EndsWith(".octopus"))
                {
                    originalFileName = originalFileName.Substring(0, originalFileName.Length - 8);
                }

                // Handle already having file extension
                string fileExtension = string.Empty;
                int lastDotIndex = originalFileName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    fileExtension = originalFileName.Substring(lastDotIndex);
                    originalFileName = originalFileName.Substring(0, lastDotIndex);
                }

                // Create the decrypted file name with "-decrypted" suffix
                string decryptedFileName = $"{originalFileName}-decrypted{fileExtension}";
                string outputPath = Path.Combine(Path.GetDirectoryName(currentSelectedFile.FilePath), decryptedFileName);

                // Decrypt the file
                bool decryptionSuccess = DecryptFileWithAES(currentSelectedFile.FilePath, outputPath, decryptionKey);

                if (decryptionSuccess)
                {
                    MessageBoxResult result = CustomMessageBox.Show(this,
                        $"File decrypted successfully and saved as:\n{decryptedFileName}\n\nWould you like to open the file?",
                        "Decryption Successful",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Open the decrypted file with the default application
                        Process.Start(outputPath);
                    }
                }
            }
            catch (CryptographicException)
            {
                CustomMessageBox.Show(this, "Incorrect decryption key provided. Please try again with the correct key.", "Decryption Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, $"Error decrypting file: {ex.Message}", "Decryption Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Implementation of file decryption
        private bool DecryptFileWithAES(string inputFile, string outputFile, string key)
        {
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);

                // Make sure key is the right length for AES-256 (32 bytes)
                using (SHA256 sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(keyBytes);
                }

                // Read the IV from the beginning of the file
                byte[] iv = new byte[16]; // AES block size = 16 bytes

                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    // Read the IV from the beginning of the file
                    if (fsInput.Read(iv, 0, iv.Length) != iv.Length)
                    {
                        throw new InvalidOperationException("Input file is too short to contain IV.");
                    }

                    // Create AES decryptor
                    using (Aes aes = Aes.Create())
                    {
                        aes.KeySize = 256;
                        aes.Key = keyBytes;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (ICryptoTransform decryptor = aes.CreateDecryptor())
                        using (FileStream fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                        {
                            // Create crypto stream to decrypt
                            using (CryptoStream cs = new CryptoStream(fsOutput, decryptor, CryptoStreamMode.Write))
                            {
                                byte[] buffer = new byte[8192]; // 8K buffer
                                int bytesRead;

                                while ((bytesRead = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    cs.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (CryptographicException)
            {
                // Specifically catch crypto exceptions which are likely due to wrong key
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Decryption error: {ex.Message}");
                return false;
            }
        }

        // Cancel decryption
        private void CancelDecrypt_Click(object sender, RoutedEventArgs e)
        {
            DecryptionDialog.Visibility = Visibility.Collapsed;
        }

        private void CopyDecryptionKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Copy the current decryption key to clipboard
                if (!string.IsNullOrEmpty(currentDecryptionKey))
                {
                    Clipboard.SetText(currentDecryptionKey);
                    CustomMessageBox.Show(this, "Decryption key copied to clipboard.", "Copy Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, $"Error copying key: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfirmKeySaved_Click(object sender, RoutedEventArgs e)
        {
            // Hide the success dialog and reset the file selection
            EncryptionSuccessDialog.Visibility = Visibility.Collapsed;
            FileSelectedState.Visibility = Visibility.Collapsed;
            FileDropZone.Visibility = Visibility.Visible;
            selectedFilePath = null;
        }
    }

    // Modified EncryptedFileInfo class to make it serializable
    public class EncryptedFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string EncryptionMethod { get; set; }
        public DateTime EncryptionDate { get; set; }
        [JsonIgnore] // Don't store the decryption key in the database for security reasons
        public string DecryptionKey { get; set; }
    }
}