using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using DNS.Server;
using System.Collections;
using System.Threading;
using System.Xml;
using MiscHelpers;
using PrivateService;
using System.Runtime.Serialization;
using PrivateAPI;

namespace PrivateWin10
{
    [Serializable()]
    [DataContract(Name = "DomainFilter", Namespace = "http://schemas.datacontract.org/")]
    public class DomainFilter
    {
        [DataMember()]
        public bool Enabled = true;
        public enum Formats
        {
            Plain = 0,
            RegExp,
            WildCard
        }
        [DataMember()]
        public Formats Format = Formats.Plain;
        [DataMember()]
        public string Domain = "";
        [DataMember()]
        public int HitCount = 0;
        [DataMember()]
        public DateTime? LastHit = null;
    }

    [Serializable()]
    [DataContract(Name = "DomainBlocklist", Namespace = "http://schemas.datacontract.org/")]
    public class DomainBlocklist
    {
        [DataMember()]
        public bool Enabled = true;
        [DataMember()]
        public string Url = "";
        [DataMember()]
        public DateTime? LastUpdate = null;
        [DataMember()]
        public int EntryCount = 0;
        [DataMember()]
        public string Status = "";
        [DataMember()]
        public string FileName = "";
    }

    public class DnsBlockList
    {
        public TimeSpan ttl = new TimeSpan(0,0,5);

        public class Level
        {
            public List<IResourceRecord> records = null;
            public Dictionary<string, Level> entries = null;
            public bool wildcard = false;
        }

        private ReaderWriterLockSlim ListLock = new ReaderWriterLockSlim();
        private Level TopLevel = new Level();

        private Dictionary<string, DomainBlocklist> Blocklists = new Dictionary<string, DomainBlocklist>();
        private UInt64 ReloadBlocklists = 0;

        public enum Lists
        {
            Undefined = 0,
            Whitelist,
            Blacklist,
        }

        private Dictionary<string, Tuple<DomainFilter, Regex>> Whitelist = new Dictionary<string, Tuple<DomainFilter, Regex>>();
        private Dictionary<string, Tuple<DomainFilter, Regex>> Blacklist = new Dictionary<string, Tuple<DomainFilter, Regex>>();

        public DnsBlockList()
        {
        }

        public IResponse ResolveLocal(Request request)
        {
            Response response = Response.FromRequest(request);

            // Note: DNS can in theory support multiple questions in one request, but its practically not supported, see:
            // https://stackoverflow.com/questions/4082081/requesting-a-and-aaaa-records-in-single-dns-query/4083071#4083071
            if (request.Questions.Count == 0)
                return response;

            Question question = request.Questions.First();
            // foreach (Question question in request.Questions)
            {
                ListLock.EnterReadLock();
                bool Blocked = false;
                if (IsBlackListed(question.Name))
                {
                    // response.AnswerRecords.Add(new IPAddressResourceRecord(question.Name, question.Type == RecordType.AAAA ? IPAddress.IPv6None : IPAddress.None, ttl));
                    Blocked = true;
                }
                else if (!IsWhiteListed(question.Name))
                {
                    /*IList<IResourceRecord> answers = Get(question);
                    if (answers != null)
                    {
                        foreach (var answer in answers)
                            response.AnswerRecords.Add(answer);
                    }*/
                    Blocked = GetRecords(question.Name, RecordType.ANY, TopLevel) != null;
                }
                ListLock.ExitReadLock();

                if (Blocked)
                {
                    // Note: new Domain("") results in invalid 
                    switch (question.Type)
                    {
                        case RecordType.CNAME:  response.AnswerRecords.Add(new CanonicalNameResourceRecord(question.Name, new Domain("null.arpa"), ttl)); break;
                        case RecordType.MX:     response.AnswerRecords.Add(new MailExchangeResourceRecord(question.Name, 0, new Domain("null.arpa"), ttl)); break;
                        case RecordType.PTR:    response.AnswerRecords.Add(new PointerResourceRecord(question.Name, new Domain("null.arpa"), ttl)); break;
                        default:                response.AnswerRecords.Add(new IPAddressResourceRecord(question.Name, question.Type == RecordType.AAAA ? IPAddress.IPv6Any : IPAddress.Any, ttl)); break;
                    }
                }
            }
   
            // todo: log

            if (response.AnswerRecords.Count == 0)
                return null;
            return response;
        }

        private void AddRecord(IResourceRecord record, Level level, int index = 1)
        {
            string[] labels = record.Name.Labels;

            if (labels.Length < index)
            {
                if (level.records == null)
                    level.records = new List<IResourceRecord>();
                level.records.Add(record);
                return;
            }

            string label = labels[labels.Length - index];
            if (label.Equals("*"))
            {
                level.wildcard = true;
                AddRecord(record, level, labels.Length + 1); // add the record here
                return;
            }
            
            if (level.entries == null)
                level.entries = new Dictionary<string, Level>();

            Level subLevel;
            if (!level.entries.TryGetValue(label, out subLevel))
            {
                subLevel = new Level();
                level.entries.Add(label, subLevel);
            }
            
            AddRecord(record, subLevel, index + 1);
        }

        public void Add(IResourceRecord entry)
        {
            AddRecord(entry, TopLevel);
        }

        public List<IResourceRecord> GetRecords(Domain domain, RecordType type, Level level, int index = 1)
        {
            string[] labels = domain.Labels;

            if (labels.Length < index || level.wildcard)
            {
                if (level.records == null)
                    return null;
                if (type == RecordType.ANY)
                    return level.records;
                return level.records.Where(e => e.Type == type).ToList();
            }

            string label = labels[labels.Length - index];

            if (level.entries == null)
                return null;

            Level subLevel;
            if (!level.entries.TryGetValue(label, out subLevel))
                return null;

            return GetRecords(domain, type, subLevel, index + 1);
        }

        public IList<IResourceRecord> Get(Question question)
        {
            return GetRecords(question.Name, question.Type, TopLevel);
        }

        /////////////////////////////////////////////////////////
        // Custom Lists

        public bool IsBlackListed(Domain domain)
        {
            return MatchList(Blacklist, domain.ToString());
        }

        public bool IsWhiteListed(Domain domain)
        {
            return MatchList(Whitelist, domain.ToString());
        }

        private bool MatchList(Dictionary<string, Tuple<DomainFilter, Regex>> DomainList, string DomainName)
        {
            foreach (var Filter in DomainList)
            {
                if (!Filter.Value.Item1.Enabled)
                    continue;
                if (Filter.Value.Item2 != null)
                {
                    if (!Filter.Value.Item2.IsMatch(DomainName))
                        continue;
                }
                else
                {
                    if (!Filter.Value.Item1.Domain.Equals(DomainName, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                Filter.Value.Item1.HitCount++;
                Filter.Value.Item1.LastHit = DateTime.Now;
                return true;
            }
            return false;
        }

        private Dictionary<string, Tuple<DomainFilter, Regex>> GetDomainFilterList(DnsBlockList.Lists List)
        {
            switch (List)
            {
                case Lists.Whitelist: return Whitelist;
                case Lists.Blacklist: return Blacklist;
            }
            return null;
        }

        public List<DomainFilter> GetDomainFilter(DnsBlockList.Lists List)
        {
            ListLock.EnterReadLock();
            var ret = GetDomainFilterList(List).Select(v => { return v.Value.Item1; }).ToList();
            ListLock.ExitReadLock();
            return ret;
        }

        public bool UpdateDomainFilter(DnsBlockList.Lists List, DomainFilter Filter)
        {
            Dictionary<string, Tuple<DomainFilter, Regex>> list = GetDomainFilterList(List);
            ListLock.EnterWriteLock();
            Tuple<DomainFilter, Regex> KnownFilter;
            if (list.TryGetValue(Filter.Domain, out KnownFilter))
            {
                KnownFilter.Item1.Enabled = Filter.Enabled;
            }
            else
                AddDomainFilterImpl(List, Filter);
            ListLock.ExitWriteLock();
            return true;
        }

        private bool AddDomainFilterImpl(DnsBlockList.Lists List, DomainFilter Filter)
        {
            // WARNING: ListLock must be locked for writing when entering this function.

            Dictionary<string, Tuple<DomainFilter, Regex>> list = GetDomainFilterList(List);

            try
            {
                Regex Matcher = null;
                if (Filter.Format == DomainFilter.Formats.RegExp)
                    Matcher = new Regex(Filter.Domain);
                else if (Filter.Format == DomainFilter.Formats.WildCard)
                    Matcher = new Regex(Regex.Escape(Filter.Domain).Replace("\\*", ".*"));
                list.Add(Filter.Domain, Tuple.Create(Filter, Matcher));
                return true;
            }
            catch {
                return false;
            }
        }

        public bool RemoveDomainFilter(DnsBlockList.Lists List, string Domain)
        {
            Dictionary<string, Tuple<DomainFilter, Regex>> list = GetDomainFilterList(List);
            ListLock.EnterWriteLock();
            var ret = list.Remove(Domain);
            ListLock.ExitWriteLock();
            return ret;
        }

        /////////////////////////////////////////////////////////
        // Blocklists

        public List<DomainBlocklist> GetDomainBlocklists()
        {
            ListLock.EnterReadLock();
            var ret = Blocklists.Values.ToList();
            ListLock.ExitReadLock();
            return ret;
        }

        public bool UpdateDomainBlocklist(DomainBlocklist Blocklist)
        {
            ListLock.EnterWriteLock();
            DomainBlocklist KnownBlocklist;
            if (Blocklists.TryGetValue(Blocklist.Url, out KnownBlocklist))
            {
                KnownBlocklist.Enabled = Blocklist.Enabled;
            }
            else
                AddDomainBlocklistImpl(Blocklist);
            ListLock.ExitWriteLock();

            ReloadBlocklists = MiscFunc.GetUTCTimeMs() + 5*1000; // schedule reaload
            return true;
        }

        private bool AddDomainBlocklistImpl(DomainBlocklist Blocklist)
        {
            // WARNING: ListLock must be locked for writing when entering this function.

            if (!Blocklists.ContainsKey(Blocklist.Url))
            {
                if (Blocklist.FileName.Length == 0)
                {
                    Blocklist.FileName = Path.GetFileName(new Uri(Blocklist.Url).LocalPath);
                    //if (Blocklist.FileName.Length == 0)
                    //    Blocklist.FileName = "blocklist";
                }

                if (Blocklist.FileName.Length > 0)
                {
                    string fileName = Blocklist.FileName;
                    for (int i = 0; ;)
                    {
                        bool Found = false;
                        foreach (DomainBlocklist blocklist in Blocklists.Values)
                        {
                            if (blocklist.FileName.Equals(Blocklist.FileName, StringComparison.OrdinalIgnoreCase))
                            {
                                Found = true;
                                break;
                            }
                        }
                        if (!Found)
                            break;

                        Blocklist.FileName = fileName + " (" + ++i + ")";
                    }
                }

                Blocklists.Add(Blocklist.Url, Blocklist);
            }
            return true;
        }

        public bool RemoveDomainBlocklist(string Url)
        {
            DomainBlocklist Blocklist = null;
            ListLock.EnterWriteLock();
            if (Blocklists.TryGetValue(Url, out Blocklist))
                Blocklists.Remove(Url);
            ListLock.ExitWriteLock();
            if (Blocklist == null)
                return false;
            
            string fileName = App.dataPath + @"\DnsBlockLists\" + Blocklist.FileName;
            FileOps.DeleteFile(fileName);

            ReloadBlocklists = MiscFunc.GetUTCTimeMs() + 5 * 1000; // schedule reaload
            return true;
        }

        /////////////////////////////////////////////////////////
        // Store/Load

        static double xmlVersion = 1;

        public bool Load()
        {
            if (!File.Exists(App.dataPath + @"\DnsBlockList.xml"))
                return false;

            ListLock.EnterWriteLock();
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(App.dataPath + @"\DnsBlockList.xml");

                double fileVersion = 0.0;
                double.TryParse(xDoc.DocumentElement.GetAttribute("Version"), out fileVersion);
                if (fileVersion != xmlVersion)
                {
                    Priv10Logger.LogError("Failed to load DNS Blocklist, unknown file version {0}, expected {1}", fileVersion, xmlVersion);
                    return false;
                }

                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    if (node.Name == "Blocklists")
                        LoadList(node);
                    else if (node.Name == "Whitelist")
                        LoadList(node, Lists.Whitelist);
                    else if (node.Name == "Blacklist")
                        LoadList(node, Lists.Blacklist);
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
            finally
            {
                ListLock.ExitWriteLock();
            }
            return true;
        }

        private void LoadList(XmlNode rootNode)
        {
            foreach (XmlNode node in rootNode)
            {
                if (node.Name != "DomainBlocklist")
                    continue;

                DomainBlocklist blocklist = new DomainBlocklist();
                foreach (XmlNode subNode in node)
                {
                    if (subNode.Name == "Url")
                        blocklist.Url = subNode.InnerText;
                    else if (subNode.Name == "Enabled")
                        bool.TryParse(subNode.InnerText, out blocklist.Enabled);
                    else if (subNode.Name == "LastUpdate")
                    {
                        DateTime dateTime;
                        if (DateTime.TryParse(subNode.InnerText, out dateTime))
                            blocklist.LastUpdate = dateTime;
                    }
                    else if (subNode.Name == "EntryCount")
                        int.TryParse(subNode.InnerText, out blocklist.EntryCount);
                    else if (subNode.Name == "FileName")
                        blocklist.FileName = subNode.InnerText;
                }
                AddDomainBlocklistImpl(blocklist);
            }
        }

        private void LoadList(XmlNode rootNode, DnsBlockList.Lists List)
        {
            foreach (XmlNode node in rootNode)
            {
                if (node.Name != "DomainFilter")
                    continue;

                DomainFilter domainFilter = new DomainFilter();
                foreach (XmlNode subNode in node)
                {
                    if (subNode.Name == "Domain")
                        domainFilter.Domain = subNode.InnerText;
                    else if (subNode.Name == "Enabled")
                        bool.TryParse(subNode.InnerText, out domainFilter.Enabled);
                    else if (subNode.Name == "Format")
                        Enum.TryParse(subNode.InnerText, out domainFilter.Format);
                    else if (subNode.Name == "HitCount")
                        int.TryParse(subNode.InnerText, out domainFilter.HitCount);
                    else if (subNode.Name == "LastHit")
                    {
                        DateTime dateTime;
                        if (DateTime.TryParse(subNode.InnerText, out dateTime))
                            domainFilter.LastHit = dateTime;
                    }
                }
                AddDomainFilterImpl(List, domainFilter);
            }
        }

        public void Store()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter writer = XmlWriter.Create(App.dataPath + @"\DnsBlockList.xml", settings);

            ListLock.EnterReadLock();

            writer.WriteStartDocument();
            writer.WriteStartElement("Blocklist");
            writer.WriteAttributeString("Version", xmlVersion.ToString());

            writer.WriteStartElement("Blocklists");
            StoreList(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("Whitelist");
            StoreList(writer, Lists.Whitelist);
            writer.WriteEndElement();

            writer.WriteStartElement("Blacklist");
            StoreList(writer, Lists.Blacklist);
            writer.WriteEndElement();

            ListLock.ExitReadLock();

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Dispose();
        }

        private void StoreList(XmlWriter writer)
        {
            foreach (DomainBlocklist blocklist in Blocklists.Values)
            {
                writer.WriteStartElement("DomainBlocklist");

                writer.WriteElementString("Url", blocklist.Url);
                writer.WriteElementString("Enabled", blocklist.Enabled.ToString());
                if (blocklist.LastUpdate != null) writer.WriteElementString("LastUpdate", blocklist.LastUpdate.ToString());
                if (blocklist.EntryCount != 0) writer.WriteElementString("EntryCount", blocklist.EntryCount.ToString());
                if (blocklist.FileName.Length > 0) writer.WriteElementString("FileName", blocklist.FileName.ToString());

                writer.WriteEndElement();
            }
        }

        private void StoreList(XmlWriter writer, DnsBlockList.Lists List)
        {
            Dictionary<string, Tuple<DomainFilter, Regex>> list = GetDomainFilterList(List);
            foreach (var item in list.Values)
            {
                DomainFilter domainfilter = item.Item1;
                writer.WriteStartElement("DomainFilter");

                writer.WriteElementString("Domain", domainfilter.Domain);
                writer.WriteElementString("Enabled", domainfilter.Enabled.ToString());
                writer.WriteElementString("Format", domainfilter.Format.ToString());
                if (domainfilter.HitCount != 0) writer.WriteElementString("HitCount", domainfilter.HitCount.ToString());
                if (domainfilter.LastHit != null) writer.WriteElementString("LastHit", domainfilter.LastHit.ToString());

                writer.WriteEndElement();
            }
        }

        /////////////////////////////////////////////////////////
        // List Loading

        public static readonly string[] DefaultLists = {
            "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
            "http://mirror1.malwaredomains.com/files/justdomains",
            "http://sysctl.org/cameleon/hosts",
            "https://s3.amazonaws.com/lists.disconnect.me/simple_tracking.txt",
            "https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt",
            "https://hosts-file.net/ad_servers.txt"
        };

        public void AddDefaultLists()
        {
            foreach (var Url in DefaultLists)
            {
                if (!Blocklists.ContainsKey(Url))
                {
                    DomainBlocklist blocklist = new DomainBlocklist() { Url = Url };
                    AddDomainBlocklistImpl(blocklist);
                }
            }
        }

        public Tuple<int, int> LoadBlockLists()
        {
            ListLock.EnterWriteLock();

            TopLevel = new Level(); // clear domain tree

            int counter = 0;
            int errors = 0;

            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                //string[] fileEntries = Directory.GetFiles(App.dataPath + @"\DnsBlockLists");
                //foreach (string fileName in fileEntries)
                foreach (DomainBlocklist blocklist in Blocklists.Values)
                {
                    if (!blocklist.Enabled)
                        continue;
                    string fileName = App.dataPath + @"\DnsBlockLists\" + blocklist.FileName;
                    if (blocklist.FileName.Length == 0 || !File.Exists(fileName))
                        continue;

                    counter++;
                    int count = LoadBlockList(fileName);
                    if (count == 0)
                        errors++;

                    blocklist.EntryCount = count;
                }
            }
            /*catch (DirectoryNotFoundException)
            {
                Priv10Logger.LogError("Could not load blocklists from {0}", App.dataPath + @"\DnsBlockLists");
            }*/
            catch (Exception err)
            {
                AppLog.Exception(err);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            AppLog.Debug("LoadBlockLists took: " + elapsedMs + "ms");

            ListLock.ExitWriteLock();

            return Tuple.Create(counter, errors);
        }

        public int LoadBlockList(string BlockListPath)
        {
            int count = 0;
            int success = 0;

            try
            {
                var lines = File.ReadAllLines(BlockListPath);
                var entries = lines.Where(l => TestLine(l)).Select(l => (ParseLine(l)));
                foreach (var entry in entries)
                {
                    count++;
                    if (entry == null)
                        continue;

                    //IPAddress ip = IPAddress.Any;
                    //if (!IPAddress.TryParse(entry.Address, out ip))
                    //    continue;
                    // Note: some blocklists use 120.0.0.1 as target, but we want always 0.0.0.0

                    Domain domain = new Domain(entry.Domain);

                    Add(new ResourceRecord(domain, new byte[0], RecordType.ANY, RecordClass.IN, ttl));
                    success++;
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }

            if (success == 0)
                Priv10Logger.LogError("Failed to load blocklist: {0}", Path.GetFileName(BlockListPath));
            else if (success < count)
                Priv10Logger.LogWarning("Loaded {1} DNS blocklist entries from {0}, {2} entries were invalid", Path.GetFileName(BlockListPath), success, count - success);
            else
                Priv10Logger.LogInfo("Loaded {1} DNS blocklist entries from {0}", Path.GetFileName(BlockListPath), success);
            return success;
        }

        static public bool TestLine(string line)
        {
            if (line.Trim().Length == 0)
                return false;
            else if (line.TrimStart().StartsWith("#"))
                return false;
            else
                return true;
        }

        static public HostsFileEntry ParseLine(string line)
        {
            // hosts file
            var match = RegexHostEntry.Match(line);
            if (match.Success)
                return new HostsFileEntry(match.Groups["domain"].Value, match.Groups["address"].Value);

            // simple domains file
            match = RegexDomanName.Match(line);
            if (match.Success)
                return new HostsFileEntry(match.Groups["domain"].Value);

            return null;
        }

        //static Regex RegexHostEntry = new Regex(@"^\s*(?<address>\S+)\s+(?<domain>\S+)\s*($|#)", RegexOptions.Compiled);
        static Regex RegexHostEntry = new Regex(@"^\s*(?<address>([0-9A-Fa-f\\.:]+))(%(lo|eth)*[0-9])*\s+(?<domain>\S+)\s*($|#)", RegexOptions.Compiled);

        static Regex RegexDomanName = new Regex(@"^\s*(?<domain>\S+)\s*($|#)", RegexOptions.Compiled);
        //static Regex RegexDomanName = new Regex(@"^\s*(?<domain>(?!\-)(xn--)?(?:[a-zA-Z\u3040-\uFFEF\d\-]{0,62}[a-zA-Z\u3040-\uFFEF\d]\.){1,126}(?!\d+)[a-zA-Z\u3040-\uFFEF\d\-]{1,63})\s*($|#)", RegexOptions.Compiled);

        public class HostsFileEntry
        {
            public string Domain;
            public string Address;

            public HostsFileEntry(string domain, string address = "0.0.0.0")
            {
                Domain = domain;
                Address = address;
            }

            public override string ToString()
            {
                return string.Format("{0} {1}", Domain, Address);
            }
        }

        /////////////////////////////////////////////////////////
        // Updater

        public void CheckForUpdates()
        {
            ListLock.EnterReadLock();
            var blocklists = Blocklists.Values.ToList();
            ListLock.ExitReadLock();

            if (ReloadBlocklists != 0 && ReloadBlocklists < MiscFunc.GetUTCTimeMs()) // in case we do more changes dont reload rightaway
            {
                ReloadBlocklists = 0;

                foreach (DomainBlocklist blocklist in blocklists)
                {
                    string fileName = App.dataPath + @"\DnsBlockLists\" + blocklist.FileName;
                    if (blocklist.Enabled && (blocklist.FileName.Length == 0 || !File.Exists(fileName)))
                        RefreshDomainBlocklist(blocklist.Url);
                }

                LoadBlockLists();
            }

            DateTime Temp = DateTime.Now.AddDays(-App.GetConfigInt("DnsProxy", "UpdateDays", 7));

            foreach (DomainBlocklist blocklist in blocklists)
            {
                if(blocklist.Enabled && (blocklist.LastUpdate == null || blocklist.LastUpdate < Temp) && !PendingDownloads.Contains(blocklist.Url))
                    RefreshDomainBlocklist(blocklist.Url);
            }
        }

        public bool RefreshDomainBlocklist(string Url = "") // empty means all
        {
            if (Url.Length == 0)
            {
                ListLock.EnterReadLock();
                var blocklists = Blocklists.Values.ToList();
                ListLock.ExitWriteLock();
                foreach (DomainBlocklist blocklist in blocklists)
                {
                    if (blocklist.Enabled && blocklist.Url.Length != 0)
                        RefreshDomainBlocklist(blocklist.Url);
                }
                return true;
            }

            if (PendingDownloads.Contains(Url))
                return true; // already in progress

            PendingDownloads.Add(Url);
            DownloadNextFile();
            return true;
        }

        private void DownloadsFinished()
        {
            LoadBlockLists();
            Priv10Logger.LogInfo("Filished Updating blocklists");
        }

        private List<string> PendingDownloads = new List<string>();
        private HttpTask mCurTask = null;

        private void DownloadNextFile()
        {
            if (PendingDownloads.Count == 0)
            {
                DownloadsFinished();
                return;
            }
            if (mCurTask != null)
                return;

            string Url = PendingDownloads.FirstOrDefault();

            DomainBlocklist Blocklist;
            ListLock.EnterReadLock();
            Blocklists.TryGetValue(Url, out Blocklist);
            ListLock.ExitReadLock();
            if (Blocklist == null)
            {
                PendingDownloads.Remove(Url);
                DownloadNextFile();
            }

            mCurTask = new HttpTask(Url, App.dataPath + @"\DnsBlockLists\", Blocklist.FileName);
            //mCurTask.Progress += OnProgress;
            mCurTask.Finished += OnFinished;
            if (!mCurTask.Start())
            {
                Priv10Logger.LogError("Failed to start download of blocklist: {0}", Url);
                PendingDownloads.Remove(Url);
                DownloadNextFile();
            }
            else
                Priv10Logger.LogInfo("Started downloading blocklist: {0}", Url);
        }

        void OnFinished(object sender, HttpTask.FinishedEventArgs args)
        {
            DomainBlocklist Blocklist;
            ListLock.EnterReadLock();
            Blocklists.TryGetValue(mCurTask.DlUrl, out Blocklist);
            ListLock.ExitReadLock();

            if (Blocklist != null)
            {
                Blocklist.LastUpdate = DateTime.Now;

                if (!args.Cancelled)
                {
                    if (!args.Success)
                    {
                        Blocklist.Status = "Download Error"; // todo localize

                        Priv10Logger.LogError("Blocklist download failed: {0}; Reason: {1}", mCurTask.DlUrl, args.GetError());
                        if (mCurTask.DlName != null && File.Exists(mCurTask.DlPath + @"\" + mCurTask.DlName))
                            Priv10Logger.LogWarning("An older version of the Blocklist is present and will be used.");
                    }
                    else
                        Blocklist.Status = "Downloaded"; // todo localize
                }
            }

            PendingDownloads.Remove(mCurTask.DlUrl);
            mCurTask = null;
            DownloadNextFile();
        }
    }
}
