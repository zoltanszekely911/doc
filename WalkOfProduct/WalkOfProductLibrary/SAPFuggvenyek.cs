using SAPIntegration;
using SAPIntegration.Classes;
using SAPIntegration.Managers;
using Serilog;
using Serilog.Core;
using SysLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WalkOfProduct.EntityModel;


namespace WalkOfProductLibrary
{

    public class SAPFuggvenyek
    {
        public static SAPIntegration.SAPIntegration SapIntegration;

        public static async Task<SAPIntegration.Classes.AlkatreszSzinkronResult> AlkatreszekSzinkronAsync(List<long> betarazandoAlkatreszIdk)
        {
            if (SapIntegration == null || !SapIntegration.IsConnectionExists())
            {
                return null;
            }
            var DC = new WalkOfProductEntities();
            SAPIntegration.Classes.AlkatreszekSzinkronRequest alkatreszekSzinkronRequest = new SAPIntegration.Classes.AlkatreszekSzinkronRequest();
            foreach (var betarazandoAlkatreszId in betarazandoAlkatreszIdk)
            {
                var alkatresz = DC.BetarazandoAlkatresz.FirstOrDefault(x => x.Id == betarazandoAlkatreszId);
                SAPIntegration.Classes.AlkatreszSzinkronRequest AlkatreszSzinkronRequest = new SAPIntegration.Classes.AlkatreszSzinkronRequest
                {
                    AlkatreszId = (int)alkatresz.Id,
                    AlkatreszVonalkodFelirat = alkatresz.AlkatreszVonalkodFelirat ?? "",
                    AlcsoportNev = alkatresz.AlkatreszCsalad?.Megnevezes ?? ""
                };

                alkatreszekSzinkronRequest.AlkatreszSzinkronRequestek.Add(AlkatreszSzinkronRequest);
            }




            SAPIntegration.Classes.AlkatreszSzinkronResult result = await SapIntegration.AlkatreszSzinkron(alkatreszekSzinkronRequest);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                MessageBox.Show(result.ErrorMessage, "SAP Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;

        }

        public static async Task<SAPIntegration.Classes.TermekSzinkronResult> TermekSzinkronAsync(Termek termek)
        {
            if (SapIntegration == null || !SapIntegration.IsConnectionExists())
            {
                return null;
            }

            SAPIntegration.SAPIntegration.TermekTipusCode termekTipusCode;

            //.ReadFrom.AppSettings()
            //.CreateLogger();

            if (termek.ReszTermek)
            {
                termekTipusCode = SAPIntegration.SAPIntegration.TermekTipusCode.FelkeszTermek;
            }
            else
            {
                termekTipusCode = SAPIntegration.SAPIntegration.TermekTipusCode.Kesztermek;
            }

            SAPIntegration.Classes.TermekSzinkronRequest TermekSzinkronRequest = new SAPIntegration.Classes.TermekSzinkronRequest
            {
                TermekId = (int)termek.Id,
                TermekMegnevezes = termek.Megnevezes,
                TermekMegnevezesIdegenNyelven = termek.MegrendeloiMegnevezes,
                TermekTipus = termekTipusCode,
                Aktiv = termek.SAPBOM.HasValue ? termek.SAPBOM.Value : true,
                Vonalkod = termek.MegrendeloiCikkszam
            };

            SAPIntegration.Classes.TermekSzinkronResult result = await SapIntegration.TermekSzinkron(TermekSzinkronRequest);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                MessageBox.Show(result.ErrorMessage, "SAP Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        public static async Task DarabjegyzekSzinkronAsync(long termekId)
        {
            var request = new DarabjegyzekRequest();
            DarabjegyzekResult result;

            if (SapIntegration == null || !SapIntegration.IsConnectionExists()) return;

            try
            {
                //Log.Logger = new LoggerConfiguration().WriteTo.File("C:\\Temp\\WOP\\ApplicationLogs\\WopApplication.db..log", rollingInterval: RollingInterval.Day).CreateLogger();
                using (var DC = new WalkOfProductEntities())
                {
                    //DC.Database.Log = Log.Information;
                    var termek = await DC.Termek.FindAsync(termekId);
                    if (termek == null || termek.Torolt == true || termek.GyartasSzunetel.GetValueOrDefault() == true)
                    {
                        MessageBox.Show("A megadott termék nem létezik, vagy törölt állapotú!", "Darabjegyzék szinkron hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var childNum = 0;
                    request.Cikkszam = "K" + termekId.ToString("D6");
                    request.Cikknev = termek.Megnevezes;
                    request.TervezesiReszleg = (SAPIntegration.SAPIntegration.TermekTervezesiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.TermekTervezesiReszleg), termek.TervezesiReszleg.GetValueOrDefault());
                    request.Mennyiseg = 1;

                    foreach (var folyamatlepes in termek.TermekGyartasiFolyamatLepes.Where(FL => !FL.Torolt).OrderBy(FL => FL.Sorszam))
                    {
                        //művelet hozzáadása
                        request.DarabjegyzekSorok.Add(new DarabjegyzekRequest.DarabjegyzekRequestSor()
                        {
                            Sorszam = childNum++,
                            SorTipus = SAPIntegration.SAPIntegration.DarabjegyzekSorTipus.pit_Resource,
                            Vonalkod = folyamatlepes.MuveletTipusKod,
                            Megnevezes = folyamatlepes.Megnevezes,
                            Muvelet = folyamatlepes.MuveletTipusKod,
                            Mennyiseg = 1M,
                            GyartasiTerulet = !folyamatlepes.MuveletiReszleg.HasValue ? (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), termek.TervezesiReszleg.GetValueOrDefault()) :
                            (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), folyamatlepes.MuveletiReszleg.GetValueOrDefault())
                        });

                        //alapanyagok hozzáadása
                        if (folyamatlepes.BeultetesiTervId.HasValue)
                        {
                            var rawList = DC.BOMItem
                                .Where(bi => bi.TermekId == termek.Id)
                                .Join(
                                    DC.BOMItemRefDes.Where(rd => rd.BeultetesiTervId == folyamatlepes.BeultetesiTervId),
                                    bi => bi.Id,
                                    rd => rd.BomItemId,
                                    (bi, rd) => new { BI = bi, RD = rd }
                                )
                                .ToList();

                            var lista = rawList
                                .GroupBy(x => new
                                {
                                    x.BI.KivalasztottAlkatreszId,
                                    x.BI.KivalasztottAlkatreszVonalkod,
                                    x.BI.KivalasztottAlkatreszDisplayText
                                })
                                .Select(g => new
                                {
                                    AlkatreszId = g.Key.KivalasztottAlkatreszId,
                                    Vonalkod = g.Key.KivalasztottAlkatreszVonalkod,
                                    Megnevezes = g.Key.KivalasztottAlkatreszDisplayText,
                                    Mennyiseg = g.Sum(x => x.RD.Mennyiseg)
                                });

                            foreach (var X in lista)
                            {
                                request.DarabjegyzekSorok.Add(new DarabjegyzekRequest.DarabjegyzekRequestSor()
                                {
                                    Sorszam = childNum++,
                                    SorTipus = SAPIntegration.SAPIntegration.DarabjegyzekSorTipus.pit_Item,
                                    Vonalkod = X.Vonalkod,
                                    Megnevezes = X.Megnevezes,
                                    Muvelet = folyamatlepes.MuveletTipusKod,
                                    Mennyiseg = X.Mennyiseg,
                                    GyartasiTerulet = !folyamatlepes.MuveletiReszleg.HasValue ? (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), termek.TervezesiReszleg.GetValueOrDefault()) :
                                    (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), folyamatlepes.MuveletiReszleg.GetValueOrDefault())
                                });
                            }
                        }

                        //beépülő termék(ek) hozzáadása
                        if (folyamatlepes.Sorszam == 1)
                        {
                            foreach (var reszTermek in termek.TermekXReszTermek.GroupBy(TXRT => new { TXRT.ReszTermekId, TXRT.Termek1.Megnevezes }).Select(RT => new { ResztermekId = RT.Key.ReszTermekId, Resztermek = RT.Key.Megnevezes, Mennyiseg = RT.Select(R => R.Id).Count() }))
                            {
                                request.DarabjegyzekSorok.Add(new DarabjegyzekRequest.DarabjegyzekRequestSor()
                                {
                                    Sorszam = childNum++,
                                    SorTipus = SAPIntegration.SAPIntegration.DarabjegyzekSorTipus.pit_Item,
                                    Vonalkod = "K" + reszTermek.ResztermekId.ToString("D6"),
                                    Megnevezes = reszTermek.Resztermek,
                                    Muvelet = folyamatlepes.MuveletTipusKod,
                                    Mennyiseg = reszTermek.Mennyiseg,
                                    GyartasiTerulet = !folyamatlepes.MuveletiReszleg.HasValue ? (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), termek.TervezesiReszleg.GetValueOrDefault()) :
                                    (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), folyamatlepes.MuveletiReszleg.GetValueOrDefault())
                                });
                            }


                            foreach (var keszTermek in termek.TermekXKeszTermek)
                            {
                                request.DarabjegyzekSorok.Add(new DarabjegyzekRequest.DarabjegyzekRequestSor()
                                {
                                    Sorszam = childNum++,
                                    SorTipus = SAPIntegration.SAPIntegration.DarabjegyzekSorTipus.pit_Item,
                                    Vonalkod = "K" + keszTermek.KeszTermekId.ToString("D6"),
                                    Megnevezes = keszTermek.Termek1.Megnevezes,
                                    Muvelet = folyamatlepes.MuveletTipusKod,
                                    Mennyiseg = 1,
                                    GyartasiTerulet = !folyamatlepes.MuveletiReszleg.HasValue ? (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), termek.TervezesiReszleg.GetValueOrDefault()) :
                                    (SAPIntegration.SAPIntegration.MuveletiReszleg)Enum.ToObject(typeof(SAPIntegration.SAPIntegration.MuveletiReszleg), folyamatlepes.MuveletiReszleg.GetValueOrDefault())
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Darabjegyzék összeállítás hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                GlobalShared.ExceptionLogolas("Darabjegyzék összeállítás hiba:", ex);
                WopLogger.Error("Darabjegyzék összeállítás hiba:", ex);
                return;
            }

            result = await SapIntegration.DarabjegyzekSzinkron(request);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                MessageBox.Show(result.ErrorMessage, "SAP Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task<GetGyartasiUtasitasokResult> GetGyartasiUtasitasok(string query)
        {
            var result = new GetGyartasiUtasitasokResult();
            if (SapIntegration == null || !SapIntegration.IsConnectionExists())
            {
                result.ErrorMessage = "Nincs kapcsolat az SAP Integration szolgáltatással!";
                return result;
            }

            try
            {
                return result = await SapIntegration.GetGyartasiUtasitasok(query);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Gyártási utasítás lekérdezés hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                GlobalShared.ExceptionLogolas("Gyártási utasítás lekérdezés hiba:", ex);
                WopLogger.Error("Gyártási utasítás lekérdezés hiba:", ex);
                result.ErrorMessage = "Gyártási utasítás lekérdezés hiba: " + ex.Message;
                return result;
            }
        }

        public static bool VanNyitottGyartasiUtasitas(string cikkszam, ref GetGyartasiUtasitasokResult eredmeny)
        {
            var task = Task.Run(() => GetGyartasiUtasitasok($"ItemCode={cikkszam}&Status=R&isHeadOnly=true"));
            eredmeny = task.Result;
            return (eredmeny.ErrorMessage == "" && eredmeny.gyartasiUtasitasok.Count > 0);
        }

        public static bool VanNyitottGyartasiUtasitas(string cikkszam)
        {
            var eredmeny = new GetGyartasiUtasitasokResult();
            return VanNyitottGyartasiUtasitas(cikkszam, ref eredmeny);
        }

        public static async Task VevoiCikkszamSzinkron(Termek termek)
        {
            var request = new MPNKatalogusSzamRequest();
            GetMPNKatalogusSzamokResult result;

            if (SapIntegration == null || !SapIntegration.IsConnectionExists()) return;

            try
            {
                request.Cikkszam = $"K{termek.Id:D6}";
                request.PartnerKod = termek.SAPVevo;
                request.VevoiCikkszam = termek.MegrendeloiCikkszam?.Length > 50 ? termek.MegrendeloiCikkszam.Substring(0, 50) : termek.MegrendeloiCikkszam;
                request.VevoiMegnevezes = termek.MegrendeloiMegnevezes?.Length > 200 ? termek.MegrendeloiMegnevezes.Substring(0, 200) : termek.MegrendeloiMegnevezes;
                result = await SapIntegration.MPNSzinkron(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "VevoiCikkszamSzinkron hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                GlobalShared.ExceptionLogolas("VevoiCikkszamSzinkron hiba:", ex);
                WopLogger.Error("VevoiCikkszamSzinkron hiba:", ex);
                return;
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                MessageBox.Show(result.ErrorMessage, "SAP Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async static Task SAPRaktarSzinkonAsync()
        {
            if (SapIntegration == null || !SapIntegration.IsConnectionExists())
            {
                return;
            }

            var x = await SAPRaktarSzinkron.Create(SapIntegration);
            await x.SendSAPRaktarSzinkronAsync();
        }

        public static async Task<GetSzallitolevelekResult> GetSzallitolevelek(Nullable<long> docNum, bool isOnlyNotClosed = false, bool isOnlyHead = false)
        {
            var result = new GetSzallitolevelekResult();
            if (SapIntegration == null || !SapIntegration.IsConnectionExists())
            {
                result.ErrorMessage = "Nincs kapcsolat az SAP Integration szolgáltatással!";
                return result;
            }

            try
            {
                return result = await SapIntegration.GetSzallitolevelek(docNum, isOnlyNotClosed, isOnlyHead);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Szállítólevél lekérdezés hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                GlobalShared.ExceptionLogolas("Szállítólevél lekérdezés hiba:", ex);
                WopLogger.Error("Szállítólevél lekérdezés hiba:", ex);
                result.ErrorMessage = "Szállítólevél lekérdezés hiba: " + ex.Message;
                return result;
            }
        }


        public static async Task SAPSendGyartasAnyagFelhasznalasKuldes(Guid nyomkovetettSzerelvenyId)
        {
            if (SapIntegration == null || !SapIntegration.IsConnectionExists()) return;

            using (var DC = new WalkOfProductEntities())
            {
                var nysz = await DC.NyomkovetettSzerelveny.FindAsync(nyomkovetettSzerelvenyId);
                if (nysz != null && nysz.Termek.SAPBOM == true)
                    _ = await SapIntegration.SendGyartasFelhasznalasJelentes(nyomkovetettSzerelvenyId);
            }
        }

        public static async Task<GetFokonyviSzamokResult> GetFokonyviSzamok(string code = "", string name = "", string u_way = "")
        {
            var result = new GetFokonyviSzamokResult();
            if (SapIntegration == null || !SapIntegration.IsConnectionExists())
            {
                result.ErrorMessage = "Nincs kapcsolat az SAP Integration szolgáltatással!";
                return result;
            }

            try
            {
                return result = await SapIntegration.GetFokonyviSzamok(code, name, u_way);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Főkönyviszám lekérdezés hiba!", MessageBoxButton.OK, MessageBoxImage.Error);
                GlobalShared.ExceptionLogolas("Főkönyviszám lekérdezés hiba:", ex);
                result.ErrorMessage = "Főkönyviszám lekérdezés hiba: " + ex.Message;
                return result;
            }
        }

    }
}
