# Introduction 
Mission of this assignment is to build software that manage a global fleet of logistics aircraft, these aircraft collect
packages (cargo) around the world and deliver it to its destination.

There are three challenges
1. Create All Rest API and run test harness to make sure planes are flying.
2. Record distance and duration covered by planes and set maintaince flag if plane reaches 50000 miles
3. Modify the Mongo shell script to include 15 largest cities in every country  with any city of gt than 1k people, get 2102 cities and then generate 200 planes, current script has only 16 planes and then verify simulation runs

## Getting started

### Expected Prerequisites 
 - C#, .NET 5.0
 - IDE - Visual Studio or VS code 
 - In this assignment - Flight plans and assignment of cargo to planes is done manually - we are
supplied with a working GUI to make these manual changes and observe what is happening.
we are not expected to write or modify any GUI components

### Component
 - Javascript GUI - It is written using HTML/CSS/Javascript and
Vue.JS framework - it expects to call web services to interact with the application. Your
web service should be able to serve the static pages for the UI as well as the RESTful
interface it calls
 - Application server - Though we have supplied with Python, Node and Java, I have written it in .Net 5.0/C#
 - Testharness - This python code will help us to verify if we have completed the REST interfaces -
once it is complete it will use it to drive the simulation of arriving cargo, moving aircraft etc
 - Data - Sample data is supplied and script to create basic data - you can change the
we have changed it to meet our goals

### Setup Process

 ```sh
1. Clone the git repo : 
2. Install latest .net 5.0 and use either Vs code or Visual Studio 
3. Open Solution File or Project File and set the mongodb connection string in the 'appsettings.Development' file
4. Run the command “dotnet MongoDb.Logistics.dll” which runs the APIs and UI if using vscode 
5. In case of Visual Studio - open MongoDb.Logistics.slm file and build it and hit F5
6. Access the url http://localhost:5000/static/index.html to see the UI 
7. Access the url http://localhost:5000/swagger/index.html - Swagger UI  for API Endpoints
8. Run the test harness to start simulation.
```

### Database 

Following Indexes are created: 

```sh
db.cities.createIndex({'position': '2dshpere'})
db.planes.createIndex({'currentLocation': '2dshpere'})
db.cargos.createIndex({status:1,location:1})
```

### Development Practice followed:

- All Services registration have its own wrapper and kept in service registration folder to make it clean - inject in startup by a single call
- Database folder contain data access layers which are stateless and injected through Mongodb registation service 
			
			#region MongoDB Injection
			services.AddSingleton<IMongoClient>(x => new MongoClient(configuration["mongoDb-connection"]));
			#endregion

			#region Mongo Repos Injection
			services.AddSingleton<ICitiesRepo, CitiesRepo>();
			services.AddSingleton<IPlanesRepo, PlanesRepo>();
			services.AddSingleton<ICargoRepo, CargoRepo>();
			#endregion

			#region MongoDB Change Stream Injection
			services.AddSingleton<CargoChangeStreamService, CargoChangeStreamService>();
			#endregion

			#region logging Service Injection
			services.AddSingleton(typeof(IAppLogger<>), typeof(LoggerAdapter<>));

			#endregion

- Database layer has repositories (cities, planes and cargo) having its own interfaces and implementation
- Used Read/write concerns as majority
- Created a seperate logging wrapper on top of microsoft in-build library - easy to customize for future 
- Two types of exception are handled 
     - System exception - mongo exceptions are captured at data access layer and then logged these error - it helps developer to know actual error while troubleshooting. These can be easily pushed to App insights or any other log store.
     - Api end point status codes - for public endpoints - various status code are defined at top of request 
     - For example or put verb
  ```sh      
		[HttpPut("{id}/delivered")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
	```
- Change stream service also been defined to track plane changes - helped to crack problem 2 
- Use thread safe locks for change stream operations
 - Created change stream to track plan landing updates and then calculate total distance aka mileage , duration and if mileages reaches more than 50000 adding flag for plan maintaince 
    - Two approaches have been followed for these calcualtion  
         1. MongoDB GeoSphere using aggregation pipeline
         2. Using Geo Cordinates nuget library to calculate distance from location coordinates
         3. Sample from plane collection after running simulation:
				
 - Best Practice for Api documentation have been followed
 - Following is the script defined for problem 3: 

  ```sh      
        minSize = {$match:{population:{$gt:1000}}}
        sortBySize = { $sort : { population: -1 }}
        groupByCountry = { $group : { _id: "$country", allCities : { $addToSet : "$$ROOT" }}}
        projectTo15orLess = { $project : { projectTo15orLess: {$slice:["$allCities",0,15]} }}
        unwind = {$unwind:{path:"$projectTo15orLess",includeArrayIndex:'false'}}
        format = { $project : { _id: { $concat: [ "$projectTo15orLess.city_ascii", "_", "$projectTo15orLess.iso2" ] } , position:["$projectTo15orLess.lng","$projectTo15orLess.lat"] , country: "$projectTo15orLess.country" }}
        newcollection = { $out : "cities" }
        db.worldcities.aggregate([minSize,sortBySize,groupByCountry,projectTo15orLess,unwind,format,newcollection])

        db.cities.find().count()
		2102

	firstSample = { $sample: { size: 200} }
	groupPlanes = { $group: { _id: null, planes : { $push : { currentLocation :"$position" }}}}
	unwind = { $unwind : {path: "$planes", includeArrayIndex: "id" }}
	format = {$project : { _id : {$concat : ["CARGO",{$toString:"$id"}]},
	currentLocation: "$planes.currentLocation", heading:{$literal:0}, route: []}}
	asplanes = { $out: "planes"}
	db.cities.aggregate([firstSample,groupPlanes,unwind,format,asplanes])
	db.planes.find().count()
		200

	```
 
