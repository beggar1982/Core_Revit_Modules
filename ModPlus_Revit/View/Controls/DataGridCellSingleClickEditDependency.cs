namespace ModPlus_Revit.View.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using ModPlusAPI.Windows.Helpers;

    /// <summary>
    /// Defines a dependency property to allow the dataGridCell to become editable in mouse single click .
    /// </summary>
    public class DataGridCellSingleClickEditDependency : DependencyObject
    {
        /// <summary>
        /// The is allow single click edit property
        /// </summary>
        public static readonly DependencyProperty IsAllowSingleClickEditProperty =
            DependencyProperty.RegisterAttached("IsAllowSingleClickEdit", typeof(bool),
                typeof(DataGridCellSingleClickEditDependency),
                new PropertyMetadata(false, IsAllowSingleClickEditChanged));

        /// <summary>
        /// Gets or sets a value indicating whether this instance is allow single click edit.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is allow single click edit; otherwise, <c>false</c>.
        /// </value>
        public bool IsAllowSingleClickEdit
        {
            get => (bool)GetValue(IsAllowSingleClickEditProperty);
            set => SetValue(IsAllowSingleClickEditProperty, value);
        }

        /// <summary>
        /// Determines whether [is allow single click edit changed] [the specified sender].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void IsAllowSingleClickEditChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is DataGridCell dataGridCell)
            {
                if (e.NewValue.Equals(true))
                {
                    dataGridCell.GotFocus += DataGridCellGotFocusHandler;
                }
                else
                {
                    dataGridCell.GotFocus -= DataGridCellGotFocusHandler;
                }
            }
        }

        /// <summary>
        /// Finds the visual parent.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        private static T FindVisualParent<T>(UIElement element) 
            where T : UIElement
        {
            var parent = element;
            while (parent != null)
            {
                if (parent is T correctlyTyped)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }

            return null;
        }

        /// <summary>
        /// Handles the GotFocus event of the AssociatedObject control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private static void DataGridCellGotFocusHandler(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }

                var dataGrid = FindVisualParent<DataGrid>(cell);

                if (dataGrid != null)
                {
                    dataGrid.BeginEdit(e);
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                        {
                            cell.IsSelected = true;
                        }
                    }
                    else
                    {
                        var row = FindVisualParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                        {
                            row.IsSelected = true;
                        }
                    }
                }

                foreach (var textBox in cell.FindChildren<TextBox>())
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }
    }
}
