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
        }

        private void AddListener(Listener listener)
        {
            syncer_.Attach(listener);
        }

        private void RemoveListener(Listener listener)
        {
            syncer_.Detach(listener);
        }

        public class ListOfOperations
        {
            public ConcurrentDictionary<string, List<JObject>> listOfOperations_ = new ConcurrentDictionary<string, List<JObject>>();
        }


        /// <summary>
        /// The 'Syncer' class
        /// </summary>
        public class Syncer
        {

            private JObject operation_ { get; set; }
            private string petrelId_ { get; set; }
            private List<IListener> listeners_ = new List<IListener>();

            private ConcurrentDictionary<string, List<JObject>> listOfOperations_ = new ConcurrentDictionary<string, List<JObject>>();


            //below is EXPERIMENTAL

            private ListOfOperations listOfOperationsExperimental_ = new ListOfOperations();
            private ConcurrentDictionary<int, ListOfOperations> indexedListOfOperations_ = new ConcurrentDictionary<int, ListOfOperations>();
            private static int index = 0;

            //above is EXPERIMENTAL


            private ConcurrentDictionary<string, List<JObject>> listOfEverything_ = new ConcurrentDictionary<string, List<JObject>>();

            // Constructor
            public Syncer()
            {
            }

            public void AddElementsToList(string id, JObject operation)
            {
                if (!listOfOperations_.ContainsKey(id))
                    listOfOperations_[id] = new List<JObject>();

                listOfOperations_[id].Add(operation);
            }

            public List<JObject> GetElementsFromList(string id)
            {
                if (!listOfOperations_.ContainsKey(id))
                    listOfOperations_[id] = new List<JObject>();

                return listOfOperations_[id];
            }

            public List<JObject> GetElementsFromListWithIndex(string id, int index)
            {
                var listOfOps = new List<JObject>();
                for (var i = index; i < indexedListOfOperations_.Count; i++)
                {
                    var keys = indexedListOfOperations_[i].listOfOperations_.Keys;
                    if (keys.Contains(id))
                        continue;

                    listOfOps = listOfOps.Union(indexedListOfOperations_[i].listOfOperations_[id]).ToList();
                }

                return listOfOps;
            }

            public void AddElementsToEverythingList(string id, JObject operation)
            {
                if (!listOfEverything_.ContainsKey(id))
                    listOfEverything_[id] = new List<JObject>();

                listOfEverything_[id].Add(operation);

                if (!listOfOperationsExperimental_.listOfOperations_.ContainsKey(id))
                    listOfOperationsExperimental_.listOfOperations_[id] = new List<JObject>();

                listOfOperationsExperimental_.listOfOperations_[id].Add(operation);
                indexedListOfOperations_[index] = listOfOperationsExperimental_;

                index++;
            }

            public List<JObject> GetElementsFromEverthingList(string id)
            {
                if (!listOfEverything_.ContainsKey(id))
                    listOfEverything_[id] = new List<JObject>();

                return listOfEverything_[id];
            }

            public void removeListElements(string id)
            {
                // this is a bit scary, but for now, i'm deleting once sending the operations to the id
                listOfOperations_[id].Clear();
            }

            public void addOperation(JObject operation, string id)
            {
                operation_ = operation;
                petrelId_ = id;
                AddElementsToEverythingList(id, operation);
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
        /// The 'Observer' pattern
        /// </summary>
        public interface IListener
        {
            void Update(JObject operation);
            string getListenerId();
        }

        /// <summary>
        /// The 'Listener' class
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
        public IActionResult GetOperations(string id)
        {
            var listOfOperations = syncer_.GetElementsFromList(id);

            var jArrayObject = JArray.FromObject(listOfOperations);

            syncer_.removeListElements(id);

            return new ObjectResult(jArrayObject);
        }

        [HttpGet("GetAllOperations/{id}")]
        public IActionResult GetAllOperations(string id)
        {
            var listOfOperations = syncer_.GetElementsFromEverthingList(id);

            var jArrayObject = JArray.FromObject(listOfOperations);

            return new ObjectResult(jArrayObject);
        }

        [HttpGet("GetAllOperations/{id}/{index}")]
        public IActionResult GetAllOperationsWithIndex(string id, int index)
        {
            var listOfOperations = syncer_.GetElementsFromListWithIndex(id, index);

            var jArrayObject = JArray.FromObject(listOfOperations);

            return new ObjectResult(jArrayObject);
        }

    }
}