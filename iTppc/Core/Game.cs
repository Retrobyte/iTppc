using iTppc.Core.Network;
using iTppc.IO;
using iTppc.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace iTppc.Core
{
    public class Game : HttpClient
    {
        private string _username;
        private string _password;
        private int _trainerId;
        private Pokemon[] _roster;
        private long _money;
        private long _teamPoints;
        private bool _banned;
        private bool _running;
        private string _refLink;
        private Statistic _stats;
        private CaptchaSolver _catpcha;
        private Random _random;
        private AutoResetEvent _monitor;
        private AutoResetEvent _finished;
        private KeyValuePair<string, string> _host;
        private KeyValuePair<string, string> _origin;
        private KeyValuePair<string, string> _ajax;
        private KeyValuePair<string, string> _acceptAjax;
        private KeyValuePair<string, string> _acceptLanguage;
        private KeyValuePair<string, string> _cacheControl;
        private KeyValuePair<string, string> _upgradeRequests;
        private KeyValuePair<string, string> _imageHost;

        public delegate void OnActivityDelegate(string act);
        public event OnActivityDelegate OnActivity;

        public delegate void OnStatisticDelegate(Statistic stat);
        public event OnStatisticDelegate OnStatistic;

        public Game()
        {
            _username = null;
            _password = null;
            _trainerId = 0;
            _roster = new Pokemon[6];
            _money = 0;
            _teamPoints = 0;
            _banned = false;
            _refLink = "http://www.tppcrpg.net/stats.php";
            _running = false;
            _stats = new Statistic();
            _catpcha = new CaptchaSolver(Properties.Resources.Captcha);
            _random = new Random();
            _monitor = new AutoResetEvent(false);
            _finished = new AutoResetEvent(false);
            _host = new KeyValuePair<string, string>("Host", "www.tppcrpg.net");
            _origin = new KeyValuePair<string, string>("Origin", "http://www.tppcrpg.net");
            _ajax = new KeyValuePair<string, string>("X-Requested-With", "XMLHttpRequest");
            _acceptAjax = new KeyValuePair<string, string>("Accept", "*/*");
            _acceptLanguage = new KeyValuePair<string, string>("Accept-Language", "en-US,en;q=0.8");
            _cacheControl = new KeyValuePair<string, string>("Cache-Control", "max-age=0");
            _upgradeRequests = new KeyValuePair<string, string>("Upgrade-Insecure-Requests", "1");
            _imageHost = new KeyValuePair<string, string>("Host", "graphics.tppcrpg.net");
            
            Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            AcceptImage = "image/webp,image/*,*/*;q=0.8";
            AutomaticDecompression = System.Net.DecompressionMethods.GZip;
        }

        public bool login(string username = null, string password = null)
        {
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(_username))
                return false;

            if (string.IsNullOrEmpty(password) && string.IsNullOrEmpty(_password))
                return false;

            AutoResetEvent waiter = new AutoResetEvent(false);

            logActivity("Navigating to login page.");

            HttpResponse main = get("http://tppcrpg.net/", null, _acceptLanguage, _host, _upgradeRequests);
            _refLink = main.Url;

            waiter.WaitOne(_random.Next(600, 800));

            HttpResponse login = get("http://www.tppcrpg.net/login.php", _refLink, _acceptLanguage, _host, _upgradeRequests);
            _refLink = login.Url;

            waiter.WaitOne(_random.Next(2800, 3200));

            HttpResponse result;

            if (login.Html.Contains("Please Enter The String Seen Below To Login:"))
            {
                do
                {
                    Bitmap captcha = (Bitmap)getImage(string.Format("http://www.tppcrpg.net/{0}", Regex.Match(login.Html, "\\<img src=\"(.+)\" alt=", RegexOptions.IgnoreCase).Groups[1].Value), "http://www.tppcrpg.net/login.php", _acceptLanguage, _host);
                    string randomImage = Regex.Match(login.Html, "RandomImage\" value=\"(\\d+)\" \\/\\>", RegexOptions.IgnoreCase).Groups[1].Value;
                    string randomCaptcha = Regex.Match(login.Html, "RandomCAPTCHA\" value=\"(\\d+)\" \\/\\>", RegexOptions.IgnoreCase).Groups[1].Value;

                    result = post("http://www.tppcrpg.net/login.php", string.Format("LoginID={0}&NewPass={1}&Validate={2}&RandomImage={3}&RandomCAPTCHA={4}", string.IsNullOrEmpty(username) ? encode(_username) : encode(username), string.IsNullOrEmpty(password) ? encode(_password) : encode(password), _catpcha.solve(captcha), randomImage, randomCaptcha), "http://www.tppcrpg.net/login.php", _acceptLanguage, _cacheControl, _host, _origin, _upgradeRequests);
                } while (result.Html.Contains("Wrong validation code entered. Please try again."));
            }
            else
            {
                result = post("http://www.tppcrpg.net/login.php", string.Format("LoginID={0}&NewPass={1}", string.IsNullOrEmpty(username) ? encode(_username) : encode(username), string.IsNullOrEmpty(password) ? encode(_password) : encode(password)), "http://www.tppcrpg.net/login.php", _acceptLanguage, _cacheControl, _host, _origin, _upgradeRequests);
            }

            if (result.Url == "http://www.tppcrpg.net/stats.php")
            {
                _username = username;
                _password = password;

                logActivity(string.Format("Logged in as {0}.", _username));

                int count = 0;

                foreach (Match match in Regex.Matches(result.Html, @"switchSidebar\(\{slot: \d, id: \'(\d+)\', name: \'([^\']+)\', level: \'(\d+)\', image: \'([^\']+)\', item: \'([^\']*)\' \}\);", RegexOptions.IgnoreCase))
                {
                    _roster[count] = new Pokemon(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[5].Value, getImage(match.Groups[4].Value, "http://www.tppcrpg.net/stats.php", _acceptLanguage, _imageHost));
                    count++;
                }

                if (count != 6)
                {
                    Image blank = getImage("http://graphics.tppcrpg.net/xy/normal/000M.gif", "http://www.tppcrpg.net/stats.php", _acceptLanguage, _imageHost);
                    
                    for (int i = count; i < 6; i++)
                        _roster[i] = new Pokemon(string.Empty, "Empty Slot", "0", "None", blank);
                }

                _trainerId = Convert.ToInt32(Regex.Match(result.Html, string.Format(@"Trainer #(\d+) - {0}", _username), RegexOptions.IgnoreCase).Groups[1].Value);
                _money = Convert.ToInt64(Regex.Match(result.Html, @"Money\<\/td\>(\n|\r|\r\n).+\$(\d{1,3}(,\d{3})*)\<\/td\>", RegexOptions.IgnoreCase).Groups[2].Value.Replace(",", string.Empty));
                _teamPoints = Convert.ToInt32(Regex.Match(result.Html, @"Team Points\<\/td\>(\n|\r|\r\n).+\>(\d{1,3}(,\d{3})*)\<\/td\>", RegexOptions.IgnoreCase).Groups[2].Value.Replace(",", string.Empty));
                _banned = result.Html.Contains("ACCOUNT BANNED");

                return true;
            }

            logActivity("Failed to login. Invalid credentials.");

            return false;
        }

        private void logActivity(string val)
        {
            string act = Logger.write(val);

            if (OnActivity != null)
                OnActivity(act);
        }

        private void updateStatistics(int money, int teamPoints, bool win, bool loss)
        {
            _stats.addMoneyGained(money);
            _stats.addTeamPointsGained(teamPoints);

            if (win)
                _stats.incrementWins();

            if (loss)
                _stats.incrementLosses();

            if (OnStatistic != null)
                OnStatistic(_stats);
        }

        public void startBattle(Settings set, SortedDictionary<int, int> trainers, Action callback = null)
        {
            if (_running)
                return;

            _running = true;

            Thread run = new Thread(() =>
            {
                AutoResetEvent waiter = new AutoResetEvent(false);
                DateTime nextInterval = DateTime.Now;
                int toWait = 0;

                while (_running && Convert.ToInt32(_roster[set.PokemonNumber - 1].Level) < set.PokemonLevel)
                {
                    if (toWait != 0)
                    {
                        logActivity(String.Format("Taking a {0} minute break.", (int)Math.Floor(toWait / 60000.0)));
                        _monitor.WaitOne(toWait);
                    }

                    if (!_running)
                        break;

                    nextInterval = DateTime.Now.AddMilliseconds(_random.Next(set.IntervalLower * 60000, set.IntervalUpper * 60000));
                    toWait = _random.Next(set.WaitLower * 60000, set.WaitUpper * 60000);

                    bool battling = true;
                    int opponentId = getOpponentId(trainers);

                    waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                    logActivity("Navigating to trainer battle page.");

                    HttpResponse toBattle = get("http://www.tppcrpg.net/battle_trainer.php", _refLink, _acceptLanguage, _host, _upgradeRequests);
                    _refLink = toBattle.Url;

                    if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                    {
                        Thread t = new Thread(() =>
                        {
                            logActivity("You have been logged out. Logging in and starting again.");
                            login();

                            if (_running)
                            {
                                _running = false;

                                startBattle(set, trainers);
                            }
                            else
                            {
                                _finished.Set();
                                _finished.Reset();
                            }
                        });

                        t.SetApartmentState(ApartmentState.STA);
                        t.IsBackground = true;
                        t.Start();

                        return;
                    }

                    waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                    battling = true;
                    logActivity(string.Format("Starting battle with trainer {0}.", opponentId));

                    HttpResponse typedTrainer = post("http://www.tppcrpg.net/battle_trainer.php", string.Format("Trainer={0}", opponentId), _refLink, _acceptLanguage, _cacheControl, _host, _origin, _upgradeRequests);
                    _refLink = typedTrainer.Url;

                    if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                    {
                        Thread t = new Thread(() =>
                        {
                            logActivity("You have been logged out. Logging in and starting again.");
                            login();

                            if (_running)
                            {
                                _running = false;

                                startBattle(set, trainers);
                            }
                            else
                            {
                                _finished.Set();
                                _finished.Reset();
                            }
                        });

                        t.SetApartmentState(ApartmentState.STA);
                        t.IsBackground = true;
                        t.Start();

                        return;
                    }

                    while (_running && Convert.ToInt32(_roster[set.PokemonNumber - 1].Level) < set.PokemonLevel)
                    {
                        if (typedTrainer.Html.Contains("Due to your tireless battling efforts, your faction has decided to reward you with bonus faction points!"))
                        {
                            logActivity("You have been chosen to be rewarded! (Captcha detected)");

                            do
                            {
                                Bitmap captcha = (Bitmap)getImage(string.Format("http://www.tppcrpg.net/{0}", Regex.Match(typedTrainer.Html, "img src=\\\"([^\\\"]+)\\\" alt=\\\"Congratulations!\\\"", RegexOptions.IgnoreCase).Groups[1].Value), _refLink);

                                waiter.WaitOne(_random.Next(1000, 2000));

                                typedTrainer = post("http://www.tppcrpg.net/bonus.php", string.Format("Validate={0}", _catpcha.solve(captcha)), _refLink, _acceptLanguage, _cacheControl, _host, _origin, _upgradeRequests);
                                _refLink = typedTrainer.Url;

                                if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                                {
                                    Thread t = new Thread(() =>
                                    {
                                        logActivity("You have been logged out. Logging in and starting again.");
                                        login();

                                        if (_running)
                                        {
                                            _running = false;

                                            startBattle(set, trainers);
                                        }
                                        else
                                        {
                                            _finished.Set();
                                            _finished.Reset();
                                        }
                                    });

                                    t.SetApartmentState(ApartmentState.STA);
                                    t.IsBackground = true;
                                    t.Start();

                                    return;
                                }

                            } while (!typedTrainer.Html.Contains("You've entered the correct code!"));

                            updateStatistics(0, Convert.ToInt32(Regex.Match(typedTrainer.Html, @"been rewarded \<strong\>(\d+)\<\/strong\> points!", RegexOptions.IgnoreCase).Groups[1].Value), false, false);
                            waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                            logActivity("Captcha solved! Renavigating to trainer battle page.");

                            toBattle = get("http://www.tppcrpg.net/battle_trainer.php", _refLink, _acceptLanguage, _host, _upgradeRequests);
                            _refLink = toBattle.Url;

                            if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                            {
                                Thread t = new Thread(() =>
                                {
                                    logActivity("You have been logged out. Logging in and starting again.");
                                    login();

                                    if (_running)
                                    {
                                        _running = false;

                                        startBattle(set, trainers);
                                    }
                                    else
                                    {
                                        _finished.Set();
                                        _finished.Reset();
                                    }
                                });

                                t.SetApartmentState(ApartmentState.STA);
                                t.IsBackground = true;
                                t.Start();

                                return;
                            }

                            waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                            battling = true;
                            logActivity(string.Format("Starting battle with trainer {0}.", opponentId));

                            typedTrainer = post("http://www.tppcrpg.net/battle_trainer.php", string.Format("Trainer={0}", opponentId), _refLink, _acceptLanguage, _cacheControl, _host, _origin, _upgradeRequests);
                            _refLink = typedTrainer.Url;

                            if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                            {
                                Thread t = new Thread(() =>
                                {
                                    logActivity("You have been logged out. Logging in and starting again.");
                                    login();

                                    if (_running)
                                    {
                                        _running = false;

                                        startBattle(set, trainers);
                                    }
                                    else
                                    {
                                        _finished.Set();
                                        _finished.Reset();
                                    }
                                });

                                t.SetApartmentState(ApartmentState.STA);
                                t.IsBackground = true;
                                t.Start();

                                return;
                            }

                            continue;
                        }

                        HttpResponse battlePage = get(string.Format("http://www.tppcrpg.net/battle.v8.handler.php?Battle=Trainer&BattleID=&Trainer={0}&Rand={1}", opponentId, getRandom()), _refLink, _acceptAjax, _acceptLanguage, _host, _ajax);

                        if (battlePage.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                        {
                            Thread t = new Thread(() =>
                            {
                                logActivity("You have been logged out. Logging in and starting again.");
                                login();

                                if (_running)
                                {
                                    _running = false;

                                    startBattle(set, trainers);
                                }
                                else
                                {
                                    _finished.Set();
                                    _finished.Reset();
                                }
                            });

                            t.SetApartmentState(ApartmentState.STA);
                            t.IsBackground = true;
                            t.Start();

                            return;
                        }

                        string battleId = getBattleId(battlePage.Html);

                        waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));

                        HttpResponse attackPage = get(string.Format("http://www.tppcrpg.net/battle.v8.handler.php?MyMove=48&pageID={0}&Battle=Trainer&BattleID={1}&Trainer={2}&Rand={3}", getButtonCoords(battlePage.Html, ClickType.Attack), battleId, opponentId, getRandom()), _refLink, _acceptAjax, _acceptLanguage, _host, _ajax);

                        if (attackPage.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                        {
                            Thread t = new Thread(() =>
                            {
                                logActivity("You have been logged out. Logging in and starting again.");
                                login();

                                if (_running)
                                {
                                    _running = false;

                                    startBattle(set, trainers);
                                }
                                else
                                {
                                    _finished.Set();
                                    _finished.Reset();
                                }
                            });

                            t.SetApartmentState(ApartmentState.STA);
                            t.IsBackground = true;
                            t.Start();

                            return;
                        }

                        while (_running || battling)
                        {
                            if (!string.IsNullOrEmpty(Regex.Match(attackPage.Html, string.Format(@"{0}\|{1} won the battle!", _trainerId, _username), RegexOptions.IgnoreCase).Value))
                            {
                                int moneyGained = Convert.ToInt32(Regex.Match(attackPage.Html, string.Format(@"{0}\|{1} gained \$(\d+)!", _trainerId, _username), RegexOptions.IgnoreCase).Groups[1].Value);
                                int teamPointsGained = 0;
                                string teamPoints = Regex.Match(attackPage.Html, string.Format(@"{0}\|{1} gained (\d+) team points for winning this battle!", _trainerId, _username), RegexOptions.IgnoreCase).Groups[1].Value;

                                if (!string.IsNullOrEmpty(teamPoints))
                                    teamPointsGained = Convert.ToInt32(teamPoints);

                                updateStatistics(moneyGained, teamPointsGained, true, false);
                                logActivity(string.Format("You have won the battle against Trainer {0}.", opponentId));

                                battling = false;

                                if (!_running)
                                    break;

                                int newId = getOpponentId(trainers);

                                if (opponentId == newId)
                                {
                                    waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                                    battling = true;
                                    logActivity(string.Format("Starting battle with trainer {0}.", opponentId));

                                    typedTrainer = get(string.Format("http://www.tppcrpg.net/battle.php?Battle=Trainer&Trainer={0}", opponentId), _refLink, _acceptLanguage, _host, _upgradeRequests);

                                    if (typedTrainer.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                                    {
                                        Thread t = new Thread(() =>
                                        {
                                            logActivity("You have been logged out. Logging in and starting again.");
                                            login();

                                            if (_running)
                                            {
                                                _running = false;

                                                startBattle(set, trainers);
                                            }
                                            else
                                            {
                                                _finished.Set();
                                                _finished.Reset();
                                            }
                                        });

                                        t.SetApartmentState(ApartmentState.STA);
                                        t.IsBackground = true;
                                        t.Start();

                                        return;
                                    }
                                }
                                else
                                {
                                    opponentId = newId;

                                    waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));

                                    toBattle = get("http://www.tppcrpg.net/battle_trainer.php", _refLink, _acceptLanguage, _host, _upgradeRequests);
                                    _refLink = toBattle.Url;

                                    if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                                    {
                                        Thread t = new Thread(() =>
                                        {
                                            logActivity("You have been logged out. Logging in and starting again.");
                                            login();

                                            if (_running)
                                            {
                                                _running = false;

                                                startBattle(set, trainers);
                                            }
                                            else
                                            {
                                                _finished.Set();
                                                _finished.Reset();
                                            }
                                        });

                                        t.SetApartmentState(ApartmentState.STA);
                                        t.IsBackground = true;
                                        t.Start();

                                        return;
                                    }

                                    waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                                    battling = true;
                                    logActivity(string.Format("Starting battle with trainer {0}.", opponentId));

                                    typedTrainer = post("http://www.tppcrpg.net/battle_trainer.php", string.Format("Trainer={0}", opponentId), _refLink, _acceptLanguage, _cacheControl, _host, _origin, _upgradeRequests);
                                    _refLink = typedTrainer.Url;

                                    if (_refLink == "http://www.tppcrpg.net/login.php?LoginRequired")
                                    {
                                        Thread t = new Thread(() =>
                                        {
                                            logActivity("You have been logged out. Logging in and starting again.");
                                            login();

                                            if (_running)
                                            {
                                                _running = false;

                                                startBattle(set, trainers);
                                            }
                                            else
                                            {
                                                _finished.Set();
                                                _finished.Reset();
                                            }
                                        });

                                        t.SetApartmentState(ApartmentState.STA);
                                        t.IsBackground = true;
                                        t.Start();

                                        return;
                                    }
                                }

                                break;
                            }
                            else if (!string.IsNullOrEmpty(Regex.Match(attackPage.Html, string.Format(@"{0}\|[^\]]+ has fainted!", _trainerId), RegexOptions.IgnoreCase).Value))
                            {
                                updateStatistics(0, 0, false, true);
                                waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                                logActivity(string.Format("You have lost the battle against Trainer {0}.", opponentId));

                                battling = false;

                                if (!_running)
                                    break;

                                attackPage = get(string.Format("http://www.tppcrpg.net/battle.v8.handler.php?MyMove=iQuit&pageID={0}&Battle=Trainer&BattleID={1}&Trainer={2}&Rand={3}", getQuitCoords(attackPage.Html), battleId, opponentId, getRandom()), _refLink, _acceptAjax, _acceptLanguage, _host, _ajax);

                                if (attackPage.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                                {
                                    Thread t = new Thread(() =>
                                    {
                                        logActivity("You have been logged out. Logging in and starting again.");
                                        login();

                                        if (_running)
                                        {
                                            _running = false;

                                            startBattle(set, trainers);
                                        }
                                        else
                                        {
                                            _finished.Set();
                                            _finished.Reset();
                                        }
                                    });

                                    t.SetApartmentState(ApartmentState.STA);
                                    t.IsBackground = true;
                                    t.Start();

                                    return;
                                }

                                waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));
                                battling = true;
                                logActivity(string.Format("Starting battle with trainer {0}.", opponentId));

                                typedTrainer = get(string.Format("http://www.tppcrpg.net/battle.php?Battle=Trainer&Trainer={0}", opponentId), _refLink, _acceptLanguage, _host, _upgradeRequests);

                                if (typedTrainer.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                                {
                                    Thread t = new Thread(() =>
                                    {
                                        logActivity("You have been logged out. Logging in and starting again.");
                                        login();

                                        if (_running)
                                        {
                                            _running = false;

                                            startBattle(set, trainers);
                                        }
                                        else
                                        {
                                            _finished.Set();
                                            _finished.Reset();
                                        }
                                    });

                                    t.SetApartmentState(ApartmentState.STA);
                                    t.IsBackground = true;
                                    t.Start();

                                    return;
                                }

                                break;
                            }
                            else if (!string.IsNullOrEmpty(Regex.Match(attackPage.Html, string.Format(@"{0}\|[^\]]+ has fainted!", opponentId), RegexOptions.IgnoreCase).Value))
                            {
                                if (!string.IsNullOrEmpty(Regex.Match(attackPage.Html, string.Format(@"{0}\|[^\]]+ has reached level \d+!", _trainerId), RegexOptions.IgnoreCase).Value))
                                {
                                    foreach (Match match in Regex.Matches(attackPage.Html, "\\<Pokemon id=\\\"T1_(\\d)\\\" CurrentHP=\\\"\\d+\\\" OriginalHP=\\\"\\d+\\\" Status=\\\"[^\\\"]*\\\" Item=\\\"[^\\\"]*\\\" Level=\\\"(\\d+)\\\""))
                                        _roster[Convert.ToInt32(match.Groups[1].Value)].Level = match.Groups[2].Value;

                                    updateStatistics(0, 0, false, false);
                                }

                                waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));

                                attackPage = get(string.Format("http://www.tppcrpg.net/battle.v8.handler.php?MyMove=WaitFaint&pageID={0}&Battle=Trainer&BattleID={1}&Trainer={2}&Rand={3}", getButtonCoords(attackPage.Html, ClickType.Continue), battleId, opponentId, getRandom()), _refLink, _acceptAjax, _acceptLanguage, _host, _ajax);

                                if (attackPage.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                                {
                                    Thread t = new Thread(() =>
                                    {
                                        logActivity("You have been logged out. Logging in and starting again.");
                                        login();

                                        if (_running)
                                        {
                                            _running = false;

                                            startBattle(set, trainers);
                                        }
                                        else
                                        {
                                            _finished.Set();
                                            _finished.Reset();
                                        }
                                    });

                                    t.SetApartmentState(ApartmentState.STA);
                                    t.IsBackground = true;
                                    t.Start();

                                    return;
                                }
                            }
                            else
                            {
                                waiter.WaitOne(_random.Next((int)Math.Floor(set.ClickLower * 1000), (int)Math.Floor(set.ClickUpper * 1000)));

                                attackPage = get(string.Format("http://www.tppcrpg.net/battle.v8.handler.php?MyMove=48&pageID={0}&Battle=Trainer&BattleID={1}&Trainer={2}&Rand={3}", getButtonCoords(attackPage.Html, ClickType.Attack), battleId, opponentId, getRandom()), _refLink, _acceptAjax, _acceptLanguage, _host, _ajax);

                                if (attackPage.Url == "http://www.tppcrpg.net/login.php?LoginRequired")
                                {
                                    Thread t = new Thread(() =>
                                    {
                                        logActivity("You have been logged out. Logging in and starting again.");
                                        login();

                                        if (_running)
                                        {
                                            _running = false;

                                            startBattle(set, trainers);
                                        }
                                        else
                                        {
                                            _finished.Set();
                                            _finished.Reset();
                                        }
                                    });

                                    t.SetApartmentState(ApartmentState.STA);
                                    t.IsBackground = true;
                                    t.Start();

                                    return;
                                }
                            }
                        }

                        if ((nextInterval - DateTime.Now).TotalMilliseconds <= 0)
                            break;
                    }
                }

                if (_running)
                {
                    if (set.PlaySound)
                        SystemSounds.Beep.Play();

                    if (set.ShowMessage)
                    {
                        Thread t = new Thread(() =>
                        {
                            MessageBox.Show("Battling has been completed!", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });

                        t.SetApartmentState(ApartmentState.STA);
                        t.IsBackground = true;
                        t.Start();
                    }
                }

                _finished.Set();

                if (_running)
                {
                    _finished.Reset();

                    _running = false;

                    if (callback != null)
                        callback();
                }
            });

            run.SetApartmentState(ApartmentState.STA);
            run.IsBackground = true;
            run.Start();
        }

        public void stopBattle(Action callback = null)
        {
            Thread run = new Thread(() =>
            {
                _running = false;

                _monitor.Set();
                _monitor.Reset();
                _finished.WaitOne();

                if (callback != null)
                    callback();
            });

            run.SetApartmentState(ApartmentState.STA);
            run.IsBackground = true;
            run.Start();
        }

        private int getOpponentId(SortedDictionary<int, int> trainers)
        {
            int ret = 1;

            foreach (int key in trainers.Keys)
            {
                if (getDamage(Convert.ToInt32(_roster[0].Level), key) >= 1)
                    ret = trainers[key];
                else
                    break;
            }
            
            return ret;
        }

        private double getDamage(int level, int blislevel)
        {
            double blishp = (((255 + 15 + 65.0) / 50) * blislevel) + 10;
            double blisdef = (((10 + 15 + 46.875) / 50) * blislevel) + 5;

            double attack = (((120 + 46.875) / 50) * level) + 5;
            double damage = ((((2 * level / 5.0 + 2) * attack * 120 / blisdef) / 50) + 2) * 1.5 * 2 * 0.85;

            return damage / blishp;
        }

        private string getBattleId(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            return xmlDoc["root"]["Battle"].Attributes["id"].Value;
        }

        private string getRandom()
        {
            return string.Format("{0}.{1}{2}", _random.Next(1000, 9999), _random.Next(100000, 999999), _random.Next(100000, 999999));
        }

        private string getButtonCoords(string xml, ClickType ct)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            int offset = (xmlDoc["root"]["Battle"]["BattleText"].ChildNodes.Count + xmlDoc["root"]["Battle"]["PostBattleText"].ChildNodes.Count + 1) * 15;

            if (ct == ClickType.Attack)
                offset += 34;
            else if (ct == ClickType.Continue)
                offset += 30;

            int x = _random.Next(629, 719);
            int y = _random.Next(431 + offset, 450 + offset);

            return string.Format("{0}.{1}", x, y);
        }

        private string getQuitCoords(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            int offset = (xmlDoc["root"]["Battle"]["BattleText"].ChildNodes.Count + xmlDoc["root"]["Battle"]["PostBattleText"].ChildNodes.Count + 3) * 15;

            int x = 0;
            int y = 0;

            if (xmlDoc["root"]["Trainer1"]["Roster"].ChildNodes.Count < 6)
            {
                x = _random.Next(434, 514);
                y = _random.Next(615 + offset, 630 + offset);
            }
            else
            {
                x = _random.Next(656, 692);
                y = _random.Next(615 + offset, 650 + offset);
            }

            return string.Format("{0}.{1}", x, y);
        }

        public Pokemon[] Roster { get { return _roster; } }

        public long Money { get { return _money; } }

        public long TeamPoints { get { return _teamPoints; } }

        public bool Running { get { return _running; } }

        public bool Banned { get { return _banned; } }
    }
}
