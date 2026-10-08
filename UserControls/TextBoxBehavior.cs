using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GRIF.UserControls
{
    public static class TextBoxBehavior
    {
        public static readonly DependencyProperty AllowPositiveNumbersProperty =
            DependencyProperty.RegisterAttached(
                "AllowPositiveNumbers",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnAllowPositiveNumbersChanged));

        public static bool GetAllowPositiveNumbers(TextBox textBox) =>
            (bool)textBox.GetValue(AllowPositiveNumbersProperty);

        public static void SetAllowPositiveNumbers(TextBox textBox, bool value) =>
            textBox.SetValue(AllowPositiveNumbersProperty, value);

        private static void OnAllowPositiveNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
                }
                else
                {
                    textBox.PreviewTextInput -= NumberTextBox_PreviewTextInput;
                }
            }
        }

        private static void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Проверяем, что вводится только цифра
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    return;
                }

                // Проверяем, что результат > 0
                var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

                if (int.TryParse(newText, out int number) && number >= 0)
                {
                    // Всё ок
                }
                else
                {
                    e.Handled = true;
                }
            }
        }
    }
}
