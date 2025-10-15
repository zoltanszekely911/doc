using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WalkOfProduct.EntityModel;

namespace WalkOfProductLibrary
{
    /// <summary>
    /// Interaction logic for DialogusKiertekeloWindow.xaml
    /// </summary>
    public partial class DialogusKiertekeloWindow : Window
    {        
        /// <summary>             
        /// Logger definiálása          
        /// </summary>
        private Serilog.ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogusKiertekeloWindow"/> class.
        /// </summary>
        public DialogusKiertekeloWindow(Serilog.ILogger logger)
        {
            InitializeComponent();
            Logger = logger;
        }

        /// <summary>
        /// Gets or Sets the kerdesek.
        /// </summary>
        public ObservableCollection<KerdoIv> Kerdesek
        {
            get { return (ObservableCollection<KerdoIv>)GetValue(KerdesekProperty); }
            set { SetValue(KerdesekProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Kerdesek.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The kerdesek property.
        /// </summary>
        public static readonly DependencyProperty KerdesekProperty =
            DependencyProperty.Register("Kerdesek",
                typeof(ObservableCollection<KerdoIv>),
                typeof(DialogusKiertekeloWindow),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or Sets the valaszok.
        /// </summary>
        public ObservableCollection<Valasz> Valaszok
        {
            get { return (ObservableCollection<Valasz>)GetValue(ValaszokProperty); }
            set { SetValue(ValaszokProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Kerdesek.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The valaszok property.
        /// </summary>
        public static readonly DependencyProperty ValaszokProperty =
            DependencyProperty.Register("Valaszok",
                typeof(ObservableCollection<Valasz>),
                typeof(DialogusKiertekeloWindow),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or Sets the felhasznalo valaszok kiertekeles.
        /// </summary>
        public ObservableCollection<FelhasznaloValaszKiertekeles> FelhasznaloValaszokKiertekeles
        {
            get { return (ObservableCollection<FelhasznaloValaszKiertekeles>)GetValue(FelhasznaloValaszokKiertekelesProperty); }
            set { SetValue(FelhasznaloValaszokKiertekelesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FelhasznaloValaszokKiertekeles.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The felhasznalo valaszok kiertekeles property.
        /// </summary>
        public static readonly DependencyProperty FelhasznaloValaszokKiertekelesProperty =
            DependencyProperty.Register("FelhasznaloValaszokKiertekeles",
                typeof(ObservableCollection<FelhasznaloValaszKiertekeles>),
                typeof(DialogusKiertekeloWindow),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or Sets the kivalasztott kerdes.
        /// </summary>
        public KerdoIv KivalasztottKerdes { get; set; }


        /// <summary>
        /// Window the loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdatokBetolteseAsync();
        }

        /// <summary>
        /// Adatok betoltese asynchronously.
        /// </summary>
        private async void AdatokBetolteseAsync()
        {
            await KerdesekBetolteseAsync();
        }

        /// <summary>
        /// Kerdesek betoltese asynchronously.
        /// </summary>
        /// <returns>A Task.</returns>
        private async Task KerdesekBetolteseAsync()
        {
            try
            {
                using (var db = new WalkOfProductEntities())
                {
                    var kerdesekDb = await db.KerdoIv.AsNoTracking().ToListAsync();
                    this.Kerdesek = new ObservableCollection<KerdoIv>(kerdesekDb);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Hiba történt a KerdesekBetolteseAsync metódusban: {ex.Message}");
            }
        }

        /// <summary>
        /// Valaszok betoltese asynchronously.
        /// </summary>
        /// <param name="kerdesId">The kerdes id.</param>
        /// <returns>A Task.</returns>
        private async Task ValaszokBetolteseAsync(long kerdesId)
        {
            try
            {
                KivalasztottKerdes = Kerdesek.SingleOrDefault(x => x.Id == kerdesId);
                if (KivalasztottKerdes == null)
                {
                    return;
                }

                Valaszok = new ObservableCollection<Valasz>(await GetValaszokAsync(kerdesId));
                FelhasznaloValaszokKiertekeles = new ObservableCollection<FelhasznaloValaszKiertekeles>();

                using (var db = new WalkOfProductEntities())
                {
                    double osszSzavazat = await GetFelhasznaloValaszokSzamaAsync(db, kerdesId);
                    foreach (var item in Valaszok)
                    {
                        var kiertekeles = new FelhasznaloValaszKiertekeles();
                        kiertekeles.Valasz = item.Szoveg;
                        var felValaszok = await GetFelhasznaloValaszokAsync(db, item.Id);
                        if (felValaszok.Count() != 0)
                        {
                            kiertekeles.SzavazatokSzama = felValaszok.Count().ToString();
                            kiertekeles.Szazalek = Math.Round((felValaszok.Count() / osszSzavazat) * 100, 1);
                        }

                        FelhasznaloValaszokKiertekeles.Add(kiertekeles);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Hiba történt a ValaszokBetolteseAsync metódusban: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the valaszok asynchronously.
        /// </summary>
        /// <param name="kerdesId">The kerdes id.</param>
        /// <returns><![CDATA[A Task<List<Valasz>>.]]></returns>
        private async Task<List<Valasz>> GetValaszokAsync(long kerdesId)
        {
            try
            {
                using (var db = new WalkOfProductEntities())
                {
                    return await db.Valasz.AsNoTracking().Where(v => v.KerdoIvId == kerdesId).ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Hiba történt a GetValaszokAsync metódusban: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the felhasznalo valaszok asynchronously.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="valaszId">The valasz id.</param>
        /// <returns><![CDATA[A Task<List<FelhasznaloValasz>>.]]></returns>
        private async Task<List<FelhasznaloValasz>> GetFelhasznaloValaszokAsync(WalkOfProductEntities db, long valaszId)
        {
            try
            {
                return await db.FelhasznaloValasz.AsNoTracking().Where(v => v.ValaszId == valaszId).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Hiba történt a GetFelhasznaloValaszokAsync metódusban: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the felhasznalo valaszok szama asynchronously.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="kerdesId">The kerdes id.</param>
        /// <returns><![CDATA[A Task<integer>.]]></returns>
        private async Task<int> GetFelhasznaloValaszokSzamaAsync(WalkOfProductEntities db, long kerdesId)
        {
            try
            {
                var felhasznaloValaszok = await db.FelhasznaloValasz.AsNoTracking().Where(v => v.KerdesId == kerdesId).ToListAsync();
                return felhasznaloValaszok.Count;
            }
            catch (Exception ex)
            {
                Logger.Error($"Hiba történt a GetFelhasznaloValaszokSzamaAsync metódusban: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Kerdes the valaszto selection changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private async void KerdesValaszto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
            {
                return;
            }

            if (comboBox.SelectedValue != null)
            {
                await ValaszokBetolteseAsync((long)(comboBox.SelectedValue));
            }

            RefreshCollectionViewSource("KerdesekCVS");
        }

        /// <summary>
        /// Refreshes the collection view source.
        /// </summary>
        /// <param name="key">The key.</param>
        private void RefreshCollectionViewSource(string key)
        {
            var kerdesekCvs = this.TryFindResource(key) as CollectionViewSource;
            if (kerdesekCvs == null || kerdesekCvs.View == null || kerdesekCvs.View.SourceCollection == null)
            {

                return;
            }

            kerdesekCvs.View.Refresh();
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
        /// Masolas the click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Masolas_Click(object sender, RoutedEventArgs e)
        {
            KerdesKeszitoWindow kerdesKeszitoWindow = new KerdesKeszitoWindow();
            kerdesKeszitoWindow.Kerdes = KivalasztottKerdes;
            kerdesKeszitoWindow.ValaszItemsSource = Valaszok;
            kerdesKeszitoWindow.Show();
        }

        /// <summary>
        /// Deaktivalas the click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The E.</param>
        private void Deaktivalas_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Biztos benne, hogy vissza szeretné vonni a kérdést?", "Figyelem!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                VisszavonasMentese();
            }
        }

        /// <summary>
        /// Visszavonas the mentese.
        /// </summary>
        private void VisszavonasMentese()
        {
            using (var db = new WalkOfProductEntities())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var kerdoIvDb = db.KerdoIv.Create();
                        kerdoIvDb.Kerdes = KivalasztottKerdes.Kerdes;
                        kerdoIvDb.KerdesKezdete = KivalasztottKerdes.KerdesKezdete;
                        kerdoIvDb.ElevulesDatuma = KivalasztottKerdes.ElevulesDatuma;
                        kerdoIvDb.CsakEgyValasz = KivalasztottKerdes.CsakEgyValasz;
                        kerdoIvDb.Visszavont = true;
                        db.KerdoIv.Add(kerdoIvDb);
                        db.SaveChanges();
                        transaction.Commit();
                        MessageBox.Show($"A kérdés visszavonása sikerült");
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
    }


    /// <summary>
    /// The felhasznalo valasz kiertekeles.
    /// </summary>
    public class FelhasznaloValaszKiertekeles
    {
        /// <summary>
        /// Gets or Sets the valasz.
        /// </summary>
        public string Valasz { get; set; }

        /// <summary>
        /// Gets or Sets the szavazatok szama.
        /// </summary>
        public string SzavazatokSzama { get; set; }

        /// <summary>
        /// Gets or Sets the szazalek.
        /// </summary>
        public double Szazalek { get; set; }
    }
}
