using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WalkOfProduct.EntityModel;

namespace WalkOfProductLibrary
{
    /// <summary>
    /// WOP Dialógus Megjelenítő Rendszer dialógus ablaka
    /// </summary>
    public partial class DialogusWindows : Window
    {
        /// <summary>
        /// The valaszok list box.
        /// </summary>
        private ListBox valaszokListBox;


        public bool bezarhato;
        public int maxValaszokSzama;
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogusWindows"/> class.
        /// </summary>
        public DialogusWindows()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or Sets the aktuális felhasználo id.
        /// </summary>
        public long AktualisFelhasznaloId
        {
            get { return (long)GetValue(AktualisFelhasznaloIdProperty); }
            set { SetValue(AktualisFelhasznaloIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for KerdoIv.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The aktualis felhasznalo id property.
        /// </summary>
        public static readonly DependencyProperty AktualisFelhasznaloIdProperty =
            DependencyProperty.Register("AktualisFelhasznaloId", typeof(long), typeof(DialogusWindows));

        /// <summary>
        /// Gets or Sets the kerdo iv.
        /// </summary>
        public WalkOfProduct.EntityModel.KerdoIv KerdoIv
        {
            get { return (WalkOfProduct.EntityModel.KerdoIv)GetValue(KerdoIvProperty); }
            set { SetValue(KerdoIvProperty, value); }
        }

        // Using a DependencyProperty as the backing store for KerdoIv.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The kerdo iv property.
        /// </summary>
        public static readonly DependencyProperty KerdoIvProperty =
            DependencyProperty.Register("KerdoIv", typeof(WalkOfProduct.EntityModel.KerdoIv), typeof(DialogusWindows));


        /// <summary>
        /// Gets or Sets a value indicating whether egy valasz.
        /// </summary>
        public bool EgyValasz { get; set; }

        /// <summary>
        /// Klikk Esemény kezelő.
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Window the loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = KerdoIv;
            this.EgyValasz = KerdoIv.CsakEgyValasz;

            if (KerdoIv.KotelezoValasz.GetValueOrDefault())
            {
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.Megsem.Visibility = Visibility.Collapsed;
                this.Topmost = true;
                this.WindowStyle = WindowStyle.None;
                bezarhato = false;
            }
            else 
            {
                bezarhato = true;
            }
        }

        /// <summary>
        /// Mégsem klikk.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Megsem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Megkeresi egy DependencyObject gyermekeit a vizuális fán.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dpObject">The dp object.</param>
        /// <returns><![CDATA[A List<T>.]]></returns>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject dpObject) where T : DependencyObject
        {
            if (dpObject == null)
            {
                yield return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dpObject); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(dpObject, i);

                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childItem in FindVisualChildren<T>(child))
                {
                    yield return childItem;
                }
            }
        }

        /// <summary>
        /// Windows the closing.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!bezarhato)
                e.Cancel = true;

            Click?.Invoke(sender, e);
        }

        /// <summary>
        /// Mentés Klikk. Létrehozza és elmenti a felhasználói válaszokat attól függően, 
        /// hogy egyszeres (RadioButton) vagy többféle (CheckBox) lehetséges.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        public void Mentes_Click(object sender, RoutedEventArgs e)
        {
            WalkOfProductEntities db = new WalkOfProductEntities();
            List<long> valaszIdk = new List<long>();
            foreach (var item in ValaszokItemControl.Items)
            {
                ListBoxItem listBoxItem = ValaszokItemControl.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (listBoxItem != null)
                {
                    if (EgyValasz)
                    {
                        var radioButton = FindVisualChildren<RadioButton>(listBoxItem).FirstOrDefault();
                        if (radioButton.IsChecked == true)
                        {
                            valaszIdk.Add((item as Valasz).Id);
                        }
                    }
                    else
                    {
                        foreach (var checkBoxItem in FindVisualChildren<CheckBox>(listBoxItem))
                        {
                            if (checkBoxItem.IsChecked == true)
                            {
                                valaszIdk.Add((item as Valasz).Id);
                            }
                        }
                    }
                }
            }

            if (valaszIdk.Count == 0) 
                return;

            foreach (var item in valaszIdk)
            {
                var felhasznaloValasz = db.FelhasznaloValasz.Create();
                felhasznaloValasz.KerdesId = KerdoIv.Id;
                felhasznaloValasz.ValaszId = item;
                felhasznaloValasz.FelhasznaloId = AktualisFelhasznaloId;

                db.FelhasznaloValasz.Add(felhasznaloValasz);
            }

            db.SaveChanges();

            bezarhato = true;
            this.Close();
        }


        private void ValasztasFrissites()
        {
            if (EgyValasz)
                return;

            var kijeloltek = new List<Valasz>();

            foreach (var item in ValaszokItemControl.Items)
            {
                ListBoxItem listBoxItem = ValaszokItemControl.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (listBoxItem != null)
                {
                    foreach (var checkBox in FindVisualChildren<CheckBox>(listBoxItem))
                    {
                        if (checkBox.IsChecked == true)
                        {
                            kijeloltek.Add(item as Valasz);
                        }
                    }
                }
            }

            bool tartozkodikKijelolve = kijeloltek.Any(v => v.Tartozkodas == true);
            bool nemTartozkodikKijelolve = kijeloltek.Any(v => v.Tartozkodas != true);
            int kijeloltDb = kijeloltek.Count;

            foreach (var item in ValaszokItemControl.Items)
            {
                ListBoxItem listBoxItem = ValaszokItemControl.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (listBoxItem != null)
                {
                    var valasz = item as Valasz;

                    bool tiltott = false;

                    // 1. Tartózkodom logika
                    if (tartozkodikKijelolve)
                        tiltott = valasz.Tartozkodas != true;
                    else if (nemTartozkodikKijelolve)
                        tiltott = valasz.Tartozkodas == true;

                    // 2. Maximum válasz szám elérése esetén tiltás
                    if (!tiltott && kijeloltDb >= maxValaszokSzama)
                    {
                        // csak a már bejelöltek maradjanak engedélyezve
                        foreach (var checkBox in FindVisualChildren<CheckBox>(listBoxItem))
                        {
                            checkBox.IsEnabled = checkBox.IsChecked == true;
                        }
                    }
                    else
                    {
                        // alapértelmezett tiltás alkalmazása
                        foreach (var checkBox in FindVisualChildren<CheckBox>(listBoxItem))
                        {
                            checkBox.IsEnabled = !tiltott || checkBox.IsChecked == true;
                        }
                    }
                }
            }
        }

        private void Valasz_Checked(object sender, RoutedEventArgs e)
        {
            ValasztasFrissites();
        }
    }
}
