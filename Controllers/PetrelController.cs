using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Observer.Controllers
{
    [Route("api/[controller]")]
    public class PetrelController : Controller
    {

        private static JObject rawJasonObject_ = new JObject();

        private static Syncer syncer_ = new Syncer();

        public PetrelController()
        {
            // Create the Syncer and add some dummy listernes for now
            // syncer_.Attach(new Listener("Petrel1"));
            // syncer_.Attach(new Listener("Petrel2"));

            // JObject tempObject = new JObject();
            // tempObject.Add("operationId", "AddOperation");

            // syncer_.addOperation(tempObject, "Petrel1");
        }

        private void AddListener(Listener listener)
        {
            syncer_.Attach(listener);
        }

        private void RemoveListener(Listener listener)
        {
            syncer_.Detach(listener);
        }

        /// <summary>
        /// The 'Syncer' class
        /// </summary>
        public class Syncer
        {

            private JObject operation_ { get; set; }
            private string petrelId_ { get; set; }
            private List<IListener> listeners_ = new List<IListener>();

            private ConcurrentDictionary<string, List<JObject>> listOfEverything_ = new ConcurrentDictionary<string, List<JObject>>();

            // Constructor
            public Syncer()
            {
            }

            public void AddElementsToList(string id, JObject operation)
            {
                if (!listOfEverything_.ContainsKey(id))
                    listOfEverything_[id] = new List<JObject>();

                listOfEverything_[id].Add(operation);
            }

            public List<JObject> GetElementsFromList(string id){
                if (!listOfEverything_.ContainsKey(id))
                    listOfEverything_[id] = new List<JObject>();

                return listOfEverything_[id];
            }

            public void addOperation(JObject operation, string id)
            {
                operation_ = operation;
                petrelId_ = id;
                Notify();
            }

            public void Attach(IListener listener)
            {
                listeners_.Add(listener);
            }

            public void Detach(IListener listener)
            {
                listeners_.Remove(listener);
            }

            public void Notify()
            {
                foreach (IListener listener in listeners_)
                {
                    var id = listener.getListenerId();
                    if (id == petrelId_)
                        continue;

                    listener.Update(operation_);
                }

                Console.WriteLine("");
            }

        }

        /// <summary>
        /// The 'Observer' interface
        /// </summary>
        public interface IListener
        {
            void Update(JObject operation);
            string getListenerId();
        }

        /// <summary>
        /// The 'ConcreteObserver' class
        /// </summary>
        public class Listener : IListener
        {
            private string guid_;

            // Constructor
            public Listener(string name)
            {
                this.guid_ = name;
            }

            public string getListenerId()
            {
                return guid_;
            }

            public void Update(JObject operation)
            {
                syncer_.AddElementsToList(guid_, operation);
                Console.WriteLine("Notified: " + guid_ + operation);
            }

        }

        [HttpPost]
        public IActionResult Create([FromBody] JObject jobject)
        {
            if (jobject == null)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost("AddOperation/{id}")]
        public IActionResult NewOperation(string id, [FromBody] JObject jobject)
        {
            if (jobject == null)
            {
                return BadRequest();
            }

            syncer_.addOperation(jobject, id);

            return Ok();
        }

        [HttpGet]
        public IActionResult EntryPoint()
        {
            return Ok();
        }

        [HttpGet("GetGuid")]
        public string GetGuidOnDemand()
        {
            var guid = Guid.NewGuid().ToString();
            syncer_.Attach(new Listener(guid));

            return guid;
        }

        [HttpGet("GetOperations/{id}")]
        public JArray GetOperations(string id)
        {
            var listOfOperations = syncer_.GetElementsFromList(id);

            var jArrayObject = JArray.FromObject(listOfOperations);

            return jArrayObject;
        }

    }
}