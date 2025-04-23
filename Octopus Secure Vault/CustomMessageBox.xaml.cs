using System;
using System.Windows;
using System.Windows.Media;

namespace Octopus_File_Vault
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        // Result property to mimic MessageBoxResult
        public MessageBoxResult Result { get; private set; }

        // Private constructor for internal use
        private CustomMessageBox()
        {
            InitializeComponent();
            Result = MessageBoxResult.None;
        }

        // Click event handlers
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = true;
        }

        // Static method to show the message box (similar to MessageBox.Show)
        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            // Create and configure the custom message box
            var messageBox = new CustomMessageBox
            {
                Owner = owner,
                MessageText = { Text = message },
                MessageTitle = { Text = title }
            };

            // Configure buttons based on MessageBoxButton
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    messageBox.OkButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.OKCancel:
                    messageBox.OkButton.Visibility = Visibility.Visible;
                    messageBox.CancelButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNo:
                    messageBox.YesButton.Visibility = Visibility.Visible;
                    messageBox.NoButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNoCancel:
                    messageBox.YesButton.Visibility = Visibility.Visible;
                    messageBox.NoButton.Visibility = Visibility.Visible;
                    messageBox.CancelButton.Visibility = Visibility.Visible;
                    break;
            }

            // Configure icon and background based on MessageBoxImage
            switch (icon)
            {
                case MessageBoxImage.Information:
                    messageBox.MessageIcon.Text = "ℹ️";
                    break;
                case MessageBoxImage.Warning:
                    messageBox.MessageIcon.Text = "⚠️";
                    break;
                case MessageBoxImage.Error:
                    messageBox.MessageIcon.Text = "❌";
                    break;
                case MessageBoxImage.Question:
                    messageBox.MessageIcon.Text = "❓";
                    break;
                default:
                    messageBox.MessageIcon.Text = "ℹ️";
                    break;
            }

            // Show the dialog and return result
            messageBox.ShowDialog();
            return messageBox.Result;
        }

        // Overloaded convenience methods
        public static MessageBoxResult Show(Window owner, string message)
        {
            return Show(owner, message, "File Locker", MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string message, string title)
        {
            return Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton buttons)
        {
            return Show(owner, message, title, buttons, MessageBoxImage.None);
        }
    }
}