using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CF.RESTClientDotNet
{
    public partial class RESTClient
    {
        #region Constructor

        public RESTClient(ISerializationAdapter serializationAdapter)
        {
            SerializationAdapter = serializationAdapter;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Make REST POST call and wait for the response
        /// </summary>
        public async Task<RESTResponse<T>> PostAsync<T>(string url, object data)
        {
            return await CallPostAsync<T>(url, data, null, TimeOutMilliseconds, ReadToEnd);
        }

        /// <summary>
        /// Make REST POST call and wait for the response
        /// </summary>
        public async Task<WebResponse> PostAsync(string url, object body)
        {
            return await CallAsync(url, body, HttpVerb.Post, TimeOutMilliseconds);
        }

        /// <summary>
        /// Make REST PUT call and wait for the response
        /// </summary>
        public async Task<RESTResponse<T>> PutAsync<T>(string url, object data)
        {
            throw new NotImplementedException("This method currently remains untested. But can be tested by uncommenting this line.");
            return await CallPutAsync<T>(url, data, null, TimeOutMilliseconds, ReadToEnd);
        }

        /// <summary>
        /// Make a GET call and wait for the response
        /// </summary>
        public async Task<RESTResponse<T>> GetAsync<T>(string url, string id)
        {
            return await CallGetAsync<T>(url, id, null, TimeOutMilliseconds, ReadToEnd);
        }

        /// <summary>
        /// Make a GET call and wait for the response
        /// </summary>
        /// 
        public async Task<RESTResponse<T>> GetAsync<T>(string url)
        {
            return await CallGetAsync<T>(url, null, null, TimeOutMilliseconds, ReadToEnd);
        }

        /// <summary>
        /// Make a GET call and wait for the response
        /// </summary>
        public async Task<RESTResponse> GetAsync(string url)
        {
            return await CallGetAsync(url, null, TimeOutMilliseconds, ReadToEnd);
        }

        #endregion

        #region Private Methods

        #region Get
        /// <summary>
        /// Make a GET call and wait for the response
        /// </summary>
        private static async Task<RESTResponse<T>> CallGetAsync<T>(string url, string id, RESTResultAction<T> responseCallback, int timeOutMilliseconds, bool readToEnd)
        {
            //Create the return value
            var retVal = new RESTResponse<T>();

            var response = await CallGetAsync(url, id, timeOutMilliseconds, readToEnd);
            retVal = await DeserialiseResponseAsync<T>(response);

            //Return the retVal
            return retVal;
        }

        /// <summary>
        /// Make a GET call and wait for the response
        /// </summary>
        private static async Task<RESTResponse> CallGetAsync(string url, string id, int timeOutMilliseconds, bool readToEnd)
        {
            var retVal = new RESTResponse();

            retVal.Response = await CallAsync(url, id, HttpVerb.Get, timeOutMilliseconds);

            //Get the stream from the server
            retVal.Data = await GetDataFromResponseStreamAsync(retVal.Response, readToEnd);

            //Return the json
            return retVal;
        }

        #endregion

        #region Base Calls

        /// <summary>
        /// Make REST call and wait for the response
        /// </summary>
        private static async Task<WebResponse> CallAsync(string url, object body, HttpVerb verb, int timeOutMilliseconds)
        {
            try
            {
                //Get the Http Request object
                var request = await GetRequestAsync(url, body, verb, timeOutMilliseconds);

                //Make the call to the server and wait for the response
                var response = await request.GetResponseAsync();

                //Return the response
                return response;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The request timed out"))
                {
                    //The REST call timed out so throw this exception
                    throw new RESTTimeoutException(timeOutMilliseconds / 1000);
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Make REST call and wait for the response with type argument
        /// </summary>

        private static async Task<RESTResponse<T>> CallAsync<T>(string url, object body, HttpVerb verb, RESTResultAction<T> responseCallback, int timeOutMilliseconds, bool readToEnd)
        {
            //Create the return value
            var retVal = new RESTResponse<T>();

            var response = await CallPostAsync<T>(url, body.ToString(), responseCallback, timeOutMilliseconds, readToEnd);
            retVal = await DeserialiseResponseAsync<T>(response);

            //Return the retVal
            return retVal;
        }

        #endregion

        #region Post
        /// <summary>
        /// Make Post call and wait for the response with type argument
        /// </summary>
        private static async Task<RESTResponse<T>> CallPostAsync<T>(string url, object data, RESTResultAction<T> responseCallback, int timeOutMilliseconds, bool readToEnd)
        {
            var response = await CallPostAsync(url, data, timeOutMilliseconds, readToEnd);
            var retVal = DeserialiseResponseAsync<T>(response);

            //Return the retVal
            return await retVal;
        }

        /// <summary>
        /// Make Post call and wait for the response
        /// </summary>
        private static async Task<RESTResponse> CallPostAsync(string url, object data, int timeOutMilliseconds, bool readToEnd)
        {
            var retVal = new RESTResponse();

            retVal.Response = await CallAsync(url, data, HttpVerb.Post, timeOutMilliseconds);

            //Get the stream from the server
            retVal.Data = await GetDataFromResponseStreamAsync(retVal.Response, readToEnd);

            //Return the json
            return retVal;
        }

        #endregion

        #region Put


        /// <summary>
        /// Make Post call and wait for the response with type argument
        /// </summary>
        private static async Task<RESTResponse<T>> CallPutAsync<T>(string url, object data, RESTResultAction<T> responseCallback, int timeOutMilliseconds, bool readToEnd)

        {
            var response = await CallPutAsync(url, data, timeOutMilliseconds, readToEnd);
            var retVal = await DeserialiseResponseAsync<T>(response);

            //Return the retVal
            return retVal;
        }

        /// <summary>
        /// Make Post call and wait for the response
        /// </summary>
        private static async Task<RESTResponse> CallPutAsync(string url, object data, int timeOutMilliseconds, bool readToEnd)
        {
            var retVal = new RESTResponse();
            retVal.Response = await CallAsync(url, data, HttpVerb.Put, timeOutMilliseconds);

            //Get the stream from the server
            retVal.Data = await GetDataFromResponseStreamAsync(retVal.Response, readToEnd);

            //Return the json
            return retVal;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates a HttpWebRequest so that the REST call can be made with it
        /// </summary>
        private static async Task<WebRequest> GetRequestAsync(string url, object argument, HttpVerb verb, int timeOutMilliseconds)
        {
            if (verb == HttpVerb.Get && argument != null)
            {
                url += "/" + argument.ToString();
            }

            //Create the web request
            var retVal = (HttpWebRequest)WebRequest.Create(new Uri(url));

            //Switch on the verb
            switch (verb)
            {
                case HttpVerb.Post:
                    retVal.Method = "POST";
                    break;
                case HttpVerb.Get:
                    retVal.Method = "GET";
                    break;
                case HttpVerb.Put:
                    retVal.Method = "PUT";
                    break;
                default:
                    throw new NotImplementedException();
            }

            //We're always going to use json
            retVal.ContentType = "application/json";

            if (verb == HttpVerb.Post || verb == HttpVerb.Put)
            {
                //Set the body of the POST/PUT

                //Serialised JSon data
                var jSon = await SerializationAdapter.SerializeAsync(argument);

                //Get the json as a byte array
                var jSonBuffer = await SerializationAdapter.DecodeStringAsync(jSon);

                using (var requestStream = await retVal.GetRequestStreamAsync())
                {
                    requestStream.Write(jSonBuffer, 0, jSonBuffer.Length);
                }
            }

            //TODO: Reimplement
            //#if (!NETFX_CORE && !SILVERLIGHT)
            //            retVal.Timeout = timeOutMilliseconds;
            //#endif

            //Return the request
            return retVal;
        }

        /// <summary>
        /// Given the response from the REST call, return the string(
        /// </summary>
        private static async Task<string> GetDataFromResponseStreamAsync(WebResponse response, bool readToEnd = false)
        {
            var responseStream = response.GetResponseStream();
            byte[] responseBuffer = null;

            if (!readToEnd)
            {
                if (responseStream.Length == -1)
                {
                    throw new Exception("An error occurred while getting data from the server. Please contact support");
                }

                //Read the stream in to a buffer
                responseBuffer = new byte[responseStream.Length];

                //Read from the stream (complete)
                var responseLength = await responseStream.ReadAsync(responseBuffer, 0, (int)responseStream.Length);
            }
            else
            {
                var reader = new StreamReader(responseStream);
                return reader.ReadToEnd();
            }

            //Convert the response from bytes to json string 
            return await SerializationAdapter.EncodeStringAsync(responseBuffer);
        }

        /// <summary>
        /// Turn a non-generic RESTResponse in to a generic one. 
        /// </summary>
        private static async Task<RESTResponse<T>> DeserialiseResponseAsync<T>(RESTResponse response)
        {
            var retVal = new RESTResponse<T>();

            //Deserialise the json to the generic type
            retVal.Data = await SerializationAdapter.DeserializeAsync<T>(response.Data);

            //Set the HttpWebResponse
            retVal.Response = response.Response;

            return retVal;
        }

        #endregion

        #endregion
    }
}