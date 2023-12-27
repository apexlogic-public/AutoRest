using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    public enum HttpVerb
    {
        /// <summary>
        /// HEAD is almost identical to GET, but without the response body.
        /// <br /><br />
        /// In other words, if GET /users returns a list of users, then HEAD /users will make the same request but will not return the list of users.
        /// <br /><br />
        /// A HEAD request is useful for checking what a GET request will return before actually making a GET request - a HEAD request can read the Content-Length header to check the size of the file, without actually downloading the file.
        /// </summary>
        HEAD,
        /// <summary>
        /// GET is used to request data from a specified resource.
        /// </summary>
        GET,
        /// <summary>
        /// POST is used to send data to a server to create/update a resource.
        /// <br /><br />
        /// The data sent to the server with POST is stored in the request body of the HTTP request.
        /// </summary>
        POST,
        /// <summary>
        /// PUT is used to send data to a server to create/update a resource.
        /// <br /><br />
        /// The difference between POST and PUT is that PUT requests are idempotent. That is, calling the same PUT request multiple times will always produce the same result. In contrast, calling a POST request repeatedly have side effects of creating the same resource multiple times.
        /// </summary>
        PUT,
        /// <summary>
        /// The DELETE method deletes the specified resource.
        /// </summary>
        DELETE,
        /// <summary>
        /// The CONNECT method is used to start a two-way communications (a tunnel) with the requested resource.
        /// </summary>
        CONNECT,
        /// <summary>
        /// The OPTIONS method describes the communication options for the target resource.
        /// </summary>
        OPTIONS,
        /// <summary>
        /// The TRACE method is used to perform a message loop-back test that tests the path for the target resource (useful for debugging purposes).
        /// </summary>
        TRACE,
        /// <summary>
        /// The PATCH method is used to apply partial modifications to a resource.
        /// </summary>
        PATCH
    }
}
