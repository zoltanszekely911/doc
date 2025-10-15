using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using WalkOfProduct.EntityModel;

namespace WalkOfProductLibrary
{
    /// <summary>
    /// Interaction logic for KerdesKeszitoWindow.xaml
    /// </summary>
    public partial class KerdesKeszitoWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KerdesKeszitoWindow"/> class.
        /// </summary>
        public KerdesKeszitoWindow()
        {
            InitializeComponent();
            FigyelmeztetesTextBlock.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for <see cref="Kerdes"/>.  
        /// This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty KerdesProperty =
            DependencyProperty.Register(
                "Kerdes",
                typeof(KerdoIv),
                typeof(KerdesKeszitoWindow),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the items source for Kerdes
        /// </summary>
        public KerdoIv Kerdes
        {
            get { return (KerdoIv)this.GetValue(KerdesProperty); }
            set { this.SetValue(KerdesProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for <see cref="ValaszItemsSource"/>.  
        /// This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ValaszItemsSourceProperty =
            DependencyProperty.Register(
                "ValaszItemsSource",
                typeof(ObservableCollection<Valasz>),
                typeof(KerdesKeszitoWindow),
                new PropertyMetadata(null));


        /// <summary>
        /// Gets or sets the items source for ValaszItemsSource
        /// </summary>
        public ObservableCollection<Valasz> ValaszItemsSource
        {
            get { return (ObservableCollection<Valasz>)this.GetValue(ValaszItemsSourceProperty); }
            set { this.SetValue(ValaszItemsSourceProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the valasz index.
        /// </summary>
        public int ValaszIndex { get; set; }

        /// <summary>
        /// Window the loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ValaszItemsSource == null)
            {
                ValaszItemsSource = new ObservableCollection<Valasz>();
            }

            ValaszIndex = ValaszItemsSource.Count - 1;
        }

        /// <summary>
        /// Megsems the click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Megsem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Add valasz item click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void AddValaszItem_Click(object sender, RoutedEventArgs e)
        {
                ValaszIndex++;
                Valasz valasz = new Valasz();
                ValaszItemsSource.Add(valasz);
                ValaszokItemControl.ItemsSource = ValaszItemsSource;
        }

        /// <summary>
        /// Remove valasz item click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void RemoveValaszItem_Click(object sender, RoutedEventArgs e)
        {
            if (ValaszItemsSource == null || ValaszItemsSource.Count == 0)
            {
                return;
            }

            ValaszItemsSource.RemoveAt(ValaszIndex);
            ValaszokItemControl.ItemsSource = ValaszItemsSource; 
            ValaszIndex--;
        }

        /// <summary>
        /// Publikalas the click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Publikalas_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(KerdesTextBox.Text))
            {
                MessageBox.Show("A kérdés mező nem lehet üres");
                return;
            }

            if (ValaszokItemControl.Items.Count == 0)
            {
                MessageBox.Show("Nincs a kérdéshez egyetlen válasz sem létrehozva");
                return;
            }

            if (ValaszokItemControl.Items.Count == 0)
            {
                MessageBox.Show("Nincs a kérdéshez egyetlen válasz sem létrehozva");
                return;
            }

            if (KerdesKezdeteDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Hiba, a kérdés kezdeti dátumat kötelező megadni!");
                return;
            }

            if (LejaratDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Hiba, a kérdés lejárati dátumat kötelező megadni!");
                return;
            }

            if (LejaratDatePicker.SelectedDate <= KerdesKezdeteDatePicker.SelectedDate)
            {
                MessageBox.Show("Hiba, A lejárati időt későbbre kell állítani, mint a kérdés kezdésének dátumát");
                return;
            }

            bool egyValasz = (bool)CsakEgyfeleValaszCheckBox.IsChecked;
            List<string> valaszok = new List<string>();
            foreach (var item in DialogusWindows.FindVisualChildren<TextBox>(ValaszokItemControl))
            {
                valaszok.Add(item.Text);
            }

            if (valaszok.Any(x => string.IsNullOrEmpty(x)))
            {
                MessageBox.Show("Egy vagy több válasz mező üres");
                return;
            }

            ValtozasokMentese(valaszok);
        }

        /// <summary>
        /// Valtozasok mentése az adatbázisba.
        /// </summary>
        /// <param name="valaszok">The valaszok.</param>
        private void ValtozasokMentese(List<string> valaszok)
        {
            using (var db = new WalkOfProductEntities())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var kerdoIvDb = db.KerdoIv.Create();
                        kerdoIvDb.Kerdes = KerdesTextBox.Text;

                        kerdoIvDb.KerdesKezdete = (KerdesKezdeteDatePicker.SelectedDate == null) ? DateTime.Now.Date :
                             (DateTime)KerdesKezdeteDatePicker.SelectedDate;
                        kerdoIvDb.ElevulesDatuma = (LejaratDatePicker.SelectedDate == null) ? kerdoIvDb.ElevulesDatuma.AddDays(30) :
                            (DateTime)LejaratDatePicker.SelectedDate;
                        kerdoIvDb.CsakEgyValasz = (bool)CsakEgyfeleValaszCheckBox.IsChecked;
                        kerdoIvDb.KotelezoValasz = (bool)KotelezoValaszCheckBox.IsChecked;

                        kerdoIvDb.MaxValaszokSzama = (kerdoIvDb.CsakEgyValasz) ? kerdoIvDb.MaxValaszokSzama = 1 :
                             MaxValaszokSzamaIntegerUpDown.Value;
                
                        kerdoIvDb.Visszavont = false;
                        db.KerdoIv.Add(kerdoIvDb);
                        db.SaveChanges();

                        foreach (var item in valaszok)
                        {
                            var valasz = db.Valasz.Create();
                            valasz.Szoveg = item;
                            valasz.KerdoIvId = kerdoIvDb.Id;
                            valasz.Tartozkodas = false;
                            db.Valasz.Add(valasz);
                        }

                        if (kerdoIvDb.KotelezoValasz.GetValueOrDefault()) 
                        {
                            Valasz tartozkodomValasz = db.Valasz.Create();
                            tartozkodomValasz.Szoveg = "Tartózkodom";
                            tartozkodomValasz.KerdoIvId = kerdoIvDb.Id;
                            tartozkodomValasz.Tartozkodas = true;
                            db.Valasz.Add(tartozkodomValasz);
                        }

                        db.SaveChanges();

                        transaction.Commit();
                        MessageBox.Show($"A kérdés mentése sikerült");
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Hiba történt a mentés során. Részletek : {ex.Message}");
                    }
                    catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Hiba, nem történt változás az adatbázisban. Részletek : {ex.Message} ");
                    }
                    finally
                    {
                        this.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Text the box text changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Text.Length % 54 == 0)
            {
                await Task.Delay(1);
                textBox.AppendText(Environment.NewLine);
                textBox.Select(textBox.Text.Length, 0);
            }
        }

        private void KotelezoValaszCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            FigyelmeztetesTextBlock.Visibility = checkBox.IsChecked.GetValueOrDefault() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CsakEgyfeleValaszCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox == null)
                return;

            Visibility visibility = checkBox.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;

            MaxValaszokSzamaIntegerUpDown.Visibility = visibility;
            MaxValaszthatoValaszokTextBlock.Visibility = visibility;
        }
    }
}
