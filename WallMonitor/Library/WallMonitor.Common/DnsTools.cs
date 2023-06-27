using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace WallMonitor.Common
{
    public static class DnsTools
    {
        /// <summary>
        /// Query a DNS Server for a domain's A record
        /// </summary>
        /// <param name="host">DNS Server</param>
        /// <param name="domain">Domain to query</param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static RecordBase? QueryARecord(IPAddress host, string domain, long timeoutMilliseconds = 1000)
        {
            RecordBase? result = null;

            var request = new DnsRequest();
            request.AddQuestion(new DnsQuestion(domain, DnsType.A, DnsClass.IN));

            var response = DnsResolver.Lookup(request, host, timeoutMilliseconds);
            if (response.Answers.Length == 0)
            {
                // no response from server
                result = null;
            }
            else
            {
                result = response.Answers[0].Record as ANameRecord;
            }
            return result;
        }

        /// <summary>
        /// Query a DNS Server for a domain's A records
        /// </summary>
        /// <param name="host">DNS Server</param>
        /// <param name="domain">Domain to query</param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static ICollection<ANameRecord> QueryARecords(IPAddress host, string domain, long timeoutMilliseconds = 1000)
        {
            var results = new List<ANameRecord>();

            var request = new DnsRequest();
            request.AddQuestion(new DnsQuestion(domain, DnsType.A, DnsClass.IN));

            var response = DnsResolver.Lookup(request, host, timeoutMilliseconds);
            if (response.Answers.Length == 0)
            {
                // no response from server
            }
            else
            {
                results.AddRange(response.Answers.Select(x => (ANameRecord)x.Record));
            }
            return results;
        }

        /// <summary>
        /// Query a DNS Server for a domain's MX records
        /// </summary>
        /// <param name="host">DNS Server</param>
        /// <param name="domain">Domain to query</param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static ICollection<MxRecord> QueryMxRecords(IPAddress host, string domain, long timeoutMilliseconds = 1000)
        {
            var results = new List<MxRecord>();

            var request = new DnsRequest();
            request.AddQuestion(new DnsQuestion(domain, DnsType.MX, DnsClass.IN));

            var response = DnsResolver.Lookup(request, host, timeoutMilliseconds);
            if (response.Answers.Length == 0)
            {
                // no response from server
            }
            else
            {
                results.AddRange(response.Answers.Select(x => (MxRecord)x.Record));
            }
            return results;
        }

        /// <summary>
        /// Query a DNS Server for a domain's SOA record
        /// </summary>
        /// <param name="host">DNS Server</param>
        /// <param name="domain">Domain to query</param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static SoaRecord? QuerySoaRecord(IPAddress host, string domain, long timeoutMilliseconds = 1000)
        {
            var request = new DnsRequest();
            request.AddQuestion(new DnsQuestion(domain, DnsType.SOA, DnsClass.IN));

            var response = DnsResolver.Lookup(request, host, timeoutMilliseconds);
            if (response.Answers.Length == 0)
            {
                // no response from server
            }
            else
            {
                return (SoaRecord?)response.Answers.Select(x => x.Record).FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Query a DNS Server for a domain's NS records
        /// </summary>
        /// <param name="host">DNS Server</param>
        /// <param name="domain">Domain to query</param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static ICollection<NsRecord> QueryNsRecords(IPAddress host, string domain, long timeoutMilliseconds = 1000)
        {
            var results = new List<NsRecord>();

            var request = new DnsRequest();
            request.AddQuestion(new DnsQuestion(domain, DnsType.NS, DnsClass.IN));

            var response = DnsResolver.Lookup(request, host, timeoutMilliseconds);
            if (response.Answers.Length == 0)
            {
                // no response from server
            }
            else
            {
                results.AddRange(response.Answers.Select(x => (NsRecord)x.Record));
            }
            return results;
        }
    }

    /// <summary>
    /// Summary description for Dns.
    /// </summary>
    internal class DnsResolver
    {
        const int DnsPort = 53;
        const int UdpRetryAttempts = 2;
        private static int _uniqueId;

        /// <summary>
        /// Private constructor - this static class should never be instantiated
        /// </summary>
        internal DnsResolver()
        {
            // no implementation
        }

        /// <summary>
        /// Shorthand form to make MX querying easier, essentially wraps up the retrieval
        /// of the MX records, and sorts them by preference
        /// </summary>
        /// <param name="domain">domain name to retrieve MX RRs for</param>
        /// <param name="dnsServer">the server we're going to ask</param>
        /// <returns>An array of MXRecords</returns>
        internal static MxRecord[] MXLookup(string domain, IPAddress dnsServer)
        {
            // check the inputs
            if (domain == null) throw new ArgumentNullException(nameof(domain));
            if (dnsServer == null) throw new ArgumentNullException(nameof(dnsServer));

            // create a request for this
            var request = new DnsRequest();

            // add one question - the MX IN lookup for the supplied domain
            request.AddQuestion(new DnsQuestion(domain, DnsType.MX, DnsClass.IN));

            // fire it off
            var response = Lookup(request, dnsServer);

            // create a growable array of MX records
            var resourceRecords = new ArrayList();

            // add each of the answers to the array
            foreach (var answer in response.Answers)
            {
                // if the answer is an MX record
                if (answer.Record.GetType() == typeof(MxRecord))
                {
                    // add it to our array
                    resourceRecords.Add(answer.Record);
                }
            }

            // create array of MX records
            var mxRecords = new MxRecord[resourceRecords.Count];

            // copy from the array list
            resourceRecords.CopyTo(mxRecords);

            // sort into lowest preference order
            Array.Sort(mxRecords);

            // and return
            return mxRecords;
        }

        /// <summary>
        /// The principal look up function, which sends a request message to the given
        /// DNS server and collects a response. This implementation re-sends the message
        /// via UDP up to two times in the event of no response/packet loss
        /// </summary>
        /// <param name="request">The logical request to send to the server</param>
        /// <param name="dnsServer">The IP address of the DNS server we are querying</param>
        /// <returns>The logical response from the DNS server or null if no response</returns>
        internal static DnsResponse Lookup(DnsRequest request, IPAddress dnsServer, long timeoutMilliseconds = 1000)
        {
            // check the inputs
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (dnsServer == null) throw new ArgumentNullException(nameof(dnsServer));

            // We will not catch exceptions here, rather just refer them to the caller

            // create an end point to communicate with
            var server = new IPEndPoint(dnsServer, DnsPort);

            // get the message
            var requestMessage = request.GetMessage();

            // send the request and get the response
            var responseMessage = UdpTransfer(server, requestMessage, timeoutMilliseconds);

            // and populate a response object from that and return it
            return new DnsResponse(responseMessage);
        }

        private static byte[] UdpTransfer(IPEndPoint server, byte[] requestMessage, long timeoutMilliseconds)
        {
            // UDP can fail - if it does try again keeping track of how many attempts we've made
            var attempts = 0;

            // try repeatedly in case of failure
            while (attempts <= UdpRetryAttempts)
            {
                // firstly, uniquely mark this request with an id
                unchecked
                {
                    // substitute in an id unique to this lookup, the request has no idea about this
                    requestMessage[0] = (byte)(_uniqueId >> 8);
                    requestMessage[1] = (byte)_uniqueId;
                }

                // we'll be send and receiving a UDP packet
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // we will wait at most timeoutMilliseconds for a dns reply
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, (int)timeoutMilliseconds);

                // send it off to the server
                socket.SendTo(requestMessage, requestMessage.Length, SocketFlags.None, server);

                // RFC1035 states that the maximum size of a UDP datagram is 512 octets (bytes)
                var responseMessage = new byte[512];

                try
                {
                    // wait for a response upto timeoutMilliseconds
                    socket.Receive(responseMessage);

                    // make sure the message returned is ours
                    if (responseMessage[0] == requestMessage[0] && responseMessage[1] == requestMessage[1])
                    {
                        // its a valid response - return it, this is our successful exit point
                        return responseMessage;
                    }
                }
                catch (SocketException)
                {
                    // failure - we better try again, but remember how many attempts
                    attempts++;
                }
                finally
                {
                    // increase the unique id
                    _uniqueId++;

                    // close the socket
                    socket.Close();
                }
            }

            // the operation has failed, this is our unsuccessful exit point
            throw new NoResponseException();
        }
    }

    internal class DnsRequest
    {
        // A request is a series of questions, an 'opcode' (RFC1035 4.1.1) and a flag to denote
        // whether recursion is required (don't ask..., just assume it is)
        private readonly ArrayList _questions;

        internal bool RecursionDesired { get; set; }

        internal Opcode Opcode { get; set; }

        /// <summary>
		/// Construct this object with the default values and create an ArrayList to hold
		/// the questions as they are added
		/// </summary>
		internal DnsRequest()
        {
            // default for a request is that recursion is desired and using standard query
            RecursionDesired = true;
            Opcode = Opcode.StandardQuery;

            // create an expandable list of questions
            _questions = new ArrayList();

        }

        /// <summary>
        /// Adds a question to the request to be sent to the DNS server.
        /// </summary>
        /// <param name="question">The question to add to the request</param>
        internal void AddQuestion(DnsQuestion question)
        {
            // abandon if null
            if (question == null) throw new ArgumentNullException(nameof(question));

            // add this question to our collection
            _questions.Add(question);
        }

        /// <summary>
        /// Convert this request into a byte array ready to send direct to the DNS server
        /// </summary>
        /// <returns></returns>
        internal byte[] GetMessage()
        {
            // construct a message for this request. This will be a byte array but we're using
            // an arraylist as we don't know how big it will be
            var data = new ArrayList();

            // the id of this message - this will be filled in by the resolver
            data.Add((byte)0);
            data.Add((byte)0);

            // write the bitfields
            data.Add((byte)(((byte)Opcode << 3) | (RecursionDesired ? 0x01 : 0)));
            data.Add((byte)0);

            // tell it how many questions
            unchecked
            {
                data.Add((byte)(_questions.Count >> 8));
                data.Add((byte)_questions.Count);
            }

            // the are no requests, name servers or additional records in a request
            data.Add((byte)0); data.Add((byte)0);
            data.Add((byte)0); data.Add((byte)0);
            data.Add((byte)0); data.Add((byte)0);

            // that's the header done - now add the questions
            foreach (DnsQuestion question in _questions)
            {
                AddDomain(data, question.Domain);
                unchecked
                {
                    data.Add((byte)0);
                    data.Add((byte)question.Type);
                    data.Add((byte)0);
                    data.Add((byte)question.Class);
                }
            }

            // and convert that to an array
            var message = new byte[data.Count];
            data.CopyTo(message);
            return message;
        }

        /// <summary>
        /// Adds a domain name to the ArrayList of bytes. This implementation does not use
        /// the domain name compression used in the class Pointer - maybe it should.
        /// </summary>
        /// <param name="data">The ArrayList representing the byte array message</param>
        /// <param name="domainName">the domain name to encode and add to the array</param>
        private static void AddDomain(ArrayList data, string domainName)
        {
            var position = 0;

            // start from the beginning and go to the end
            while (position < domainName.Length)
            {
                // look for a period, after where we are
                var length = domainName.IndexOf('.', position) - position;

                // if there isn't one then this labels length is to the end of the string
                if (length < 0) length = domainName.Length - position;

                // add the length
                data.Add((byte)length);

                // copy a char at a time to the array
                while (length-- > 0)
                {
                    data.Add((byte)domainName[position++]);
                }

                // step over '.'
                position++;
            }

            // end of domain names
            data.Add((byte)0);
        }
    }

    public class DnsResponse
    {
        // these are fields we're interested in from the message
        private readonly DnsQuestion[] _questions;
        private readonly DnsAnswer[] _answers;
        private readonly NameServer[] _nameServers;
        private readonly DnsAdditionalRecord[] _additionalRecords;

        // these fields are readonly outside the assembly - use r/o properties
        internal ReturnCode ReturnCode { get; }

        internal bool AuthoritativeAnswer { get; }
        internal bool RecursionAvailable { get; }
        internal bool MessageTruncated { get; }
        internal DnsQuestion[] Questions => _questions;
        internal DnsAnswer[] Answers => _answers;
        internal NameServer[] NameServers => _nameServers;
        internal DnsAdditionalRecord[] AdditionalRecords => _additionalRecords;

        /// <summary>
		/// Construct a Response object from the supplied byte array
		/// </summary>
		/// <param name="message">a byte array returned from a DNS server query</param>
		internal DnsResponse(byte[] message)
        {
            // the bit flags are in bytes 2 and 3
            var flags1 = message[2];
            var flags2 = message[3];

            // get return code from lowest 4 bits of byte 3
            var returnCode = flags2 & 15;

            // if its in the reserved section, set to other
            if (returnCode > 6) returnCode = 6;
            ReturnCode = (ReturnCode)returnCode;

            // other bit flags
            AuthoritativeAnswer = ((flags1 & 4) != 0);
            RecursionAvailable = ((flags2 & 128) != 0);
            MessageTruncated = ((flags1 & 2) != 0);

            // create the arrays of response objects
            _questions = new DnsQuestion[GetShort(message, 4)];
            _answers = new DnsAnswer[GetShort(message, 6)];
            _nameServers = new NameServer[GetShort(message, 8)];
            _additionalRecords = new DnsAdditionalRecord[GetShort(message, 10)];

            // need a pointer to do this, position just after the header
            var pointer = new DnsPointer(message, 12);

            // and now populate them, they always follow this order
            for (var index = 0; index < _questions.Length; index++)
            {
                try
                {
                    // try to build a quesion from the response
                    _questions[index] = new DnsQuestion(pointer);
                }
                catch (Exception ex)
                {
                    // something grim has happened, we can't continue
                    throw new InvalidResponseException(ex);
                }
            }
            for (var index = 0; index < _answers.Length; index++)
            {
                _answers[index] = new DnsAnswer(pointer);
            }
            for (var index = 0; index < _nameServers.Length; index++)
            {
                _nameServers[index] = new NameServer(pointer);
            }
            for (var index = 0; index < _additionalRecords.Length; index++)
            {
                _additionalRecords[index] = new DnsAdditionalRecord(pointer);
            }
        }

        /// <summary>
        /// Convert 2 bytes to a short. It would have been nice to use BitConverter for this,
        /// it however reads the bytes in the wrong order (at least on Windows)
        /// </summary>
        /// <param name="message">byte array to look in</param>
        /// <param name="position">position to look at</param>
        /// <returns>short representation of the two bytes</returns>
        private static short GetShort(byte[] message, int position)
        {
            return (short)(message[position] << 8 | message[position + 1]);
        }
    }

    /// <summary>
    /// Represents a DNS Question, comprising of a domain to query, the type of query (QTYPE) and the class
    /// of query (QCLASS). This class is an encapsulation of these three things, and extensive argument checking
    /// in the constructor as this may well be created outside the assembly (public protection)
    /// </summary>
    [Serializable]
    public class DnsQuestion
    {
        // A question is these three things combined
        private readonly string _domain;
        private readonly DnsType _dnsType;
        private readonly DnsClass _dnsClass;

        // expose them read/only to the world
        internal string Domain => _domain;
        internal DnsType Type => _dnsType;
        internal DnsClass Class => _dnsClass;

        /// <summary>
		/// Construct the question from parameters, checking for safety
		/// </summary>
		/// <param name="domain">the domain name to query eg. bigdevelopments.co.uk</param>
		/// <param name="dnsType">the QTYPE of query eg. DnsType.MX</param>
		/// <param name="dnsClass">the CLASS of query, invariably DnsClass.IN</param>
		public DnsQuestion(string domain, DnsType dnsType, DnsClass dnsClass)
        {
            // check the input parameters
            if (domain == null) throw new ArgumentNullException(nameof(domain));

            // do a sanity check on the domain name to make sure its legal
            if (domain.Length == 0 || domain.Length > 255 || !Regex.IsMatch(domain, @"^[a-z|A-Z|0-9|-|_]{1,63}(\.[a-z|A-Z|0-9|-|_]{1,63})+$"))
            {
                // domain names can't be bigger tan 255 chars, and individual labels can't be bigger than 63 chars
                throw new ArgumentException("The supplied domain name was not in the correct form", nameof(domain));
            }

            // sanity check the DnsType parameter
            if (!Enum.IsDefined(typeof(DnsType), dnsType) || dnsType == DnsType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(dnsType), "Not a valid value");
            }

            // sanity check the DnsClass parameter
            if (!Enum.IsDefined(typeof(DnsClass), dnsClass) || dnsClass == DnsClass.None)
            {
                throw new ArgumentOutOfRangeException(nameof(dnsClass), "Not a valid value");
            }

            // just remember the values
            _domain = domain;
            _dnsType = dnsType;
            _dnsClass = dnsClass;
        }

        /// <summary>
        /// Construct the question reading from a DNS Server response. Consult RFC1035 4.1.2
        /// for byte-wise details of this structure in byte array form
        /// </summary>
        /// <param name="pointer">a logical pointer to the Question in byte array form</param>
        internal DnsQuestion(DnsPointer pointer)
        {
            // extract from the message
            _domain = pointer.ReadDomain();
            _dnsType = (DnsType)pointer.ReadShort();
            _dnsClass = (DnsClass)pointer.ReadShort();
        }
    }

    /// <summary>
    /// Represents a Resource Record as detailed in RFC1035 4.1.3
    /// </summary>
    [Serializable]
    public class DnsResourceRecord
    {
        // private, constructor initialized fields
        private readonly string _domain;
        private readonly DnsType _dnsType;
        private readonly DnsClass _dnsClass;
        private readonly int _Ttl;
        private readonly RecordBase _record;

        // read only properties applicable for all records
        public string Domain => _domain;
        public DnsType Type => _dnsType;
        public DnsClass Class => _dnsClass;
        public int Ttl => _Ttl;
        public RecordBase Record => _record;

        /// <summary>
		/// Construct a resource record from a pointer to a byte array
		/// </summary>
		/// <param name="pointer">the position in the byte array of the record</param>
		public DnsResourceRecord(DnsPointer pointer)
        {
            // extract the domain, question type, question class and Ttl
            _domain = pointer.ReadDomain();
            _dnsType = (DnsType)pointer.ReadShort();
            _dnsClass = (DnsClass)pointer.ReadShort();
            _Ttl = pointer.ReadInt();

            // the next short is the record length, we only use it for unrecognized record types
            int recordLength = pointer.ReadShort();

            // and create the appropriate RDATA record based on the dnsType
            switch (_dnsType)
            {
                case DnsType.NS: _record = new NsRecord(pointer); break;
                case DnsType.MX: _record = new MxRecord(pointer); break;
                case DnsType.A: _record = new ANameRecord(pointer); break;
                case DnsType.SOA: _record = new SoaRecord(pointer); break;
                default:
                    {
                        // move the pointer over this unrecognized record
                        pointer += recordLength;
                        break;
                    }
            }
        }
    }

    // Answers, Name Servers and Additional Records all share the same RR format
    [Serializable]
    public class DnsAnswer : DnsResourceRecord
    {
        internal DnsAnswer(DnsPointer pointer) : base(pointer) { }
    }

    [Serializable]
    public class NameServer : DnsResourceRecord
    {
        internal NameServer(DnsPointer pointer) : base(pointer) { }
    }

    [Serializable]
    public class DnsAdditionalRecord : DnsResourceRecord
    {
        internal DnsAdditionalRecord(DnsPointer pointer) : base(pointer) { }
    }

    /// <summary>
    /// Logical representation of a pointer, but in fact a byte array reference and a position in it. This
    /// is used to read logical units (bytes, shorts, integers, domain names etc.) from a byte array, keeping
    /// the pointer updated and pointing to the next record. This type of Pointer can be considered the logical
    /// equivalent of an (unsigned char*) in C++
    /// </summary>
    public class DnsPointer
    {
        // a pointer is a reference to the message and an index
        private readonly byte[] _message;
        private int _position;

        // pointers can only be created by passing on an existing message
        public DnsPointer(byte[] message, int position)
        {
            _message = message;
            _position = position;
        }

        /// <summary>
        /// Shallow copy function
        /// </summary>
        /// <returns></returns>
        public DnsPointer Copy()
        {
            return new DnsPointer(_message, _position);
        }

        /// <summary>
        /// Adjust the pointers position within the message
        /// </summary>
        /// <param name="position">new position in the message</param>
        public void SetPosition(int position)
        {
            _position = position;
        }

        /// <summary>
        /// Overloads the + operator to allow advancing the pointer by so many bytes
        /// </summary>
        /// <param name="pointer">the initial pointer</param>
        /// <param name="offset">the offset to add to the pointer in bytes</param>
        /// <returns>a reference to a new pointer moved forward by offset bytes</returns>
        public static DnsPointer operator +(DnsPointer pointer, int offset)
        {
            return new DnsPointer(pointer._message, pointer._position + offset);
        }

        /// <summary>
        /// Reads a single byte at the current pointer, does not advance pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public byte Peek()
        {
            return _message[_position];
        }

        /// <summary>
        /// Reads a single byte at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public byte ReadByte()
        {
            return _message[_position++];
        }

        /// <summary>
        /// Reads two bytes to form a short at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public short ReadShort()
        {
            return (short)(ReadByte() << 8 | ReadByte());
        }

        /// <summary>
        /// Reads four bytes to form a int at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public int ReadInt()
        {
            return (ushort)ReadShort() << 16 | (ushort)ReadShort();
        }

        /// <summary>
        /// Reads a single byte as a char at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public char ReadChar()
        {
            return (char)ReadByte();
        }

        /// <summary>
        /// Reads a domain name from the byte array. The method by which this works is described
        /// in RFC1035 - 4.1.4. Essentially to minimise the size of the message, if part of a domain
        /// name already been seen in the message, rather than repeating it, a pointer to the existing
        /// definition is used. Each word in a domain name is a label, and is preceded by its length
        /// 
        /// eg. bigdevelopments.co.uk
        /// 
        /// is [15] (size of bigdevelopments) + "bigdevelopments"
        ///    [2]  "co"
        ///    [2]  "uk"
        ///    [1]  0 (NULL)
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public string ReadDomain()
        {
            var domain = new StringBuilder();
            var length = 0;

            // get  the length of the first label
            while ((length = ReadByte()) != 0)
            {
                // top 2 bits set denotes domain name compression and to reference elsewhere
                if ((length & 0xc0) == 0xc0)
                {
                    // work out the existing domain name, copy this pointer
                    var newPointer = Copy();

                    // and move it to where specified here
                    newPointer.SetPosition((length & 0x3f) << 8 | ReadByte());

                    // repeat call recursively
                    domain.Append(newPointer.ReadDomain());
                    return domain.ToString();
                }

                // if not using compression, copy a char at a time to the domain name
                while (length > 0)
                {
                    domain.Append(ReadChar());
                    length--;
                }

                // if size of next label isn't null (end of domain name) add a period ready for next label
                if (Peek() != 0) domain.Append('.');
            }

            // and return
            return domain.ToString();
        }
    }

    /// <summary>
    /// Thrown when the server delivers a response we are not expecting to hear
    /// </summary>
    [Serializable]
    public class InvalidResponseException : SystemException
    {
        public InvalidResponseException()
        {
            // no implementation
        }

        public InvalidResponseException(Exception innerException)
            : base(null, innerException)
        {
            // no implementation
        }

        public InvalidResponseException(string message, Exception innerException)
            : base(message, innerException)
        {
            // no implementation
        }

        protected InvalidResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // no implementation
        }
    }

    /// <summary>
    /// Thrown when the server does not respond
    /// </summary>
    [Serializable]
    public class NoResponseException : SystemException
    {
        public NoResponseException()
        {
            // no implementation
        }

        public NoResponseException(Exception innerException)
            : base(null, innerException)
        {
            // no implementation
        }

        public NoResponseException(string message, Exception innerException)
            : base(message, innerException)
        {
            // no implementation
        }

        protected NoResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // no implementation
        }
    }

    /// <summary>
    /// A simple base class for the different ResourceRecords, ANAME, MX, SOA, NS etc.
    /// </summary>
    public abstract class RecordBase
    {
        // no implementation
    }


    /// <summary>
    /// An MX (Mail Exchanger) Resource Record (RR) (RFC1035 3.3.9)
    /// </summary>
    [Serializable]
    public class MxRecord : RecordBase, IComparable
    {
        // an MX record is a domain name and an integer preference
        private readonly string _domainName;
        private readonly int _preference;

        // expose these fields public read/only
        public string DomainName => _domainName;
        public int Preference => _preference;

        /// <summary>
		/// Constructs an MX record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal MxRecord(DnsPointer pointer)
        {
            _preference = pointer.ReadShort();
            _domainName = pointer.ReadDomain();
        }

        public override string ToString()
        {
            return string.Format("Mail Server = {0}, Preference = {1}", _domainName, _preference.ToString());
        }

        #region IComparable Members

        /// <summary>
        /// Implements the IComparable interface so that we can sort the MX records by their
        /// lowest preference
        /// </summary>
        /// <param name="other">the other MxRecord to compare against</param>
        /// <returns>1, 0, -1</returns>
        public int CompareTo(object obj)
        {
            var mxOther = (MxRecord)obj;

            // we want to be able to sort them by preference
            if (mxOther._preference < _preference) return 1;
            if (mxOther._preference > _preference) return -1;

            // order mail servers of same preference by name
            return -mxOther._domainName.CompareTo(_domainName);
        }

        public static bool operator ==(MxRecord record1, MxRecord record2)
        {
            if (record1 == null) throw new ArgumentNullException(nameof(record1));

            return record1.Equals(record2);
        }

        public static bool operator !=(MxRecord record1, MxRecord record2)
        {
            return !(record1 == record2);
        }
        /*
				public static bool operator<(MXRecord record1, MXRecord record2)
				{
					if (record1._preference > record2._preference) return false;
					if (record1._domainName > record2._domainName) return false;
					return false;
				}

				public static bool operator>(MXRecord record1, MXRecord record2)
				{
					if (record1._preference < record2._preference) return false;
					if (record1._domainName < record2._domainName) return false;
					return false;
				}
		*/

        public override bool Equals(object? obj)
        {
            // this object isn't null
            if (obj == null) return false;

            // must be of same type
            if (this.GetType() != obj.GetType()) return false;

            var mxOther = (MxRecord)obj;

            // preference must match
            if (mxOther._preference != _preference) return false;

            // and so must the domain name
            if (mxOther._domainName != _domainName) return false;

            // its a match
            return true;
        }

        public override int GetHashCode()
        {
            return _preference;
        }

        #endregion
    }

    /// <summary>
    /// A Name Server Resource Record (RR) (RFC1035 3.3.11)
    /// </summary>
    public class NsRecord : RecordBase
    {
        // the fields exposed outside the assembly
        private readonly string _domainName;

        // expose this domain name address r/o to the world
        public string DomainName => _domainName;

        /// <summary>
		/// Constructs a NS record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal NsRecord(DnsPointer pointer)
        {
            _domainName = pointer.ReadDomain();
        }

        public override string ToString()
        {
            return _domainName;
        }
    }

    /// <summary>
    /// ANAME Resource Record (RR) (RFC1035 3.4.1)
    /// </summary>
    public class ANameRecord : RecordBase
    {
        // An ANAME records consists simply of an IP address
        internal IPAddress _ipAddress;

        // expose this IP address r/o to the world
        public IPAddress IPAddress => _ipAddress;

        /// <summary>
		/// Constructs an ANAME record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal ANameRecord(DnsPointer pointer)
        {
            var b1 = pointer.ReadByte();
            var b2 = pointer.ReadByte();
            var b3 = pointer.ReadByte();
            var b4 = pointer.ReadByte();

            // this next line's not brilliant - couldn't find a better way though
            _ipAddress = IPAddress.Parse($"{b1}.{b2}.{b3}.{b4}");
        }

        public override string ToString()
        {
            return _ipAddress.ToString();
        }
    }

    /// <summary>
    /// An SOA Resource Record (RR) (RFC1035 3.3.13)
    /// </summary>
    public class SoaRecord : RecordBase
    {
        // these fields constitute an SOA RR
        private readonly string _primaryNameServer;
        private readonly string _responsibleMailAddress;
        private readonly int _serial;
        private readonly int _refresh;
        private readonly int _retry;
        private readonly int _expire;
        private readonly int _defaultTtl;

        // expose these fields public read/only
        public string PrimaryNameServer => _primaryNameServer;
        public string ResponsibleMailAddress => _responsibleMailAddress;
        public int Serial => _serial;
        public int Refresh => _refresh;
        public int Retry => _retry;
        public int Expire => _expire;
        public int DefaultTtl => _defaultTtl;

        /// <summary>
		/// Constructs an SOA record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal SoaRecord(DnsPointer pointer)
        {
            // read all fields RFC1035 3.3.13
            _primaryNameServer = pointer.ReadDomain();
            _responsibleMailAddress = pointer.ReadDomain();
            _serial = pointer.ReadInt();
            _refresh = pointer.ReadInt();
            _retry = pointer.ReadInt();
            _expire = pointer.ReadInt();
            _defaultTtl = pointer.ReadInt();
        }

        public override string ToString()
        {
            return string.Format("primary name server = {0}\nresponsible mail addr = {1}\nserial  = {2}\nrefresh = {3}\nretry   = {4}\nexpire  = {5}\ndefault TTL = {6}",
                _primaryNameServer,
                _responsibleMailAddress,
                _serial.ToString(),
                _refresh.ToString(),
                _retry.ToString(),
                _expire.ToString(),
                _defaultTtl.ToString());
        }
    }

    /// <summary>
    /// The DNS CLASS (RFC1035 3.2.4/5)
    /// Internet will be the one we'll be using (IN), the others are for completeness
    /// </summary>
    public enum DnsClass
    {
        None = 0,
        IN = 1, /*the Internet*/
        CS = 2, /*CSNet class*/
        CH = 3, /*Chaos class*/
        HS = 4 /*Hesiod*/
    }

    /// <summary>
    /// The DNS QType values (RFC1035 3.2.3)
    /// </summary>
    public enum DnsQType
    {
        AXFR = 252,
        MAILB = 253,
        MAILA = 254,
        Any = 255
    }

    /// <summary>
    /// (RFC1035 4.1.1) These are the return codes the server can send back
    /// </summary>
    public enum ReturnCode
    {
        Success = 0,
        FormatError = 1,
        ServerFailure = 2,
        NameError = 3,
        NotImplemented = 4,
        Refused = 5,
        Other = 6
    }

    /// <summary>
    /// The DNS Record TYPE (RFC1035 3.2.2)
    /// </summary>
    public enum DnsType
    {
        None = 0,
        A = 1, /*Host address*/
        NS = 2, /*Name server*/
        MD = 3, /*mail destination (obsolete)*/
        MF = 4, /*mail forwarder (obsolete)*/
        CNAME = 5, /*canonical name for an alias*/
        SOA = 6,  /*start of a zon authority*/
        MB = 7, /*mailbox domain name*/
        MG = 8, /*mail group member*/
        MR = 9, /*mail rename domain name*/
        NULL = 10, /*null RR*/
        WKS = 11, /*well known service description*/
        PTR = 12, /*domain name ptr to IP*/
        HINFO = 13, /*host information*/
        MINFO = 14, /*mailbox information*/
        MX = 15, /*mail exchange*/
        TXT = 16 /*text string*/
    }

    /// <summary>
    /// (RFC1035 4.1.1) These are the Query Types which apply to all questions in a request
    /// </summary>
    public enum Opcode
    {
        StandardQuery = 0, /*a standard query*/
        InverseQuerty = 1, /*an inverse query*/
        StatusRequest = 2, /*a server status request*/
        /*future use*/
        Reserverd3 = 3,
        Reserverd4 = 4,
        Reserverd5 = 5,
        Reserverd6 = 6,
        Reserverd7 = 7,
        Reserverd8 = 8,
        Reserverd9 = 9,
        Reserverd10 = 10,
        Reserverd11 = 11,
        Reserverd12 = 12,
        Reserverd13 = 13,
        Reserverd14 = 14,
        Reserverd15 = 15,
    }
}
