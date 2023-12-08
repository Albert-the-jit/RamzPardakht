echo "Enter your migration name:"
read msg

cd src/RamzPardakht.Infrastructure
dotnet ef --startup-project ../RamzPardakht.WebApi/ migrations add $msg -- --provider Postgresql

cd ../RamzPardakht.SqliteMigrations
dotnet ef --startup-project ../RamzPardakht.WebApi/ migrations add $msg -- --provider Sqlite
read
