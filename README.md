# MembershipRegister
Generic membership register.
The Register allows for creating, modifying, deleting and searching members.
The Register saves a members memberID, firstname, lastname, date of birth, address, phone number, email, groups that the member belongs to and position in those groups to a database.

# MembershipRegisterServer
A https server for handling user requests and managing the database.
The server takes in requests send to https://localhost:8001/Member
The request body must be in JSON format.
For creating a new member the format would be: 
{
	"id": "id",
	"firstname": "firstname",
	"lastname": "lastname",
	"birthdate": "12.9.1990",
	"address": "Street 1",
	"phone": "0400123456",
	"email": "user.email @for - contacting.com",
	"team:0": "Team",
	"position:0": "Position",
	"team:1": "Team",
	"position:1": "Position",
	"team:2": "Team",
	"position:2": "Position"
}

The https server requires a certificate to function.
In Windows 10 a self-signed certificate can be generated and linked to a port by doing the following:

Run Powershell as administrator.

Create self-signed Certificate
$Certificate=New-SelfSignedCertificate –Subject localhost -CertStoreLocation Cert:\LocalMachine\My

Look up the Certificate thumbprint
$Certificate

Link the Certificate to the used port
netsh http add sslcert ipport=0.0.0.0:{0} certhash={1} appid={2}

Where {0} is your port. Code uses: 8001

Where {1} is your certificate thumbprint.

Where {2} is a GUID that represents your project. {C87B09F6-1EE5-4BD0-8448-26D1D40D0EC3} for this project.

GUID can be found from the projects solution file as Unique project GUID

Project("{project type GUID}") = "<Project name>", "<project file location>", "{<Unique project GUID>}"