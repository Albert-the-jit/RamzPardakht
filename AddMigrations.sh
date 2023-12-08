cd src/RamzPardakht.Infrastructure
echo "Enter your migration name:"
read msg
dotnet-ef --startup-project ../RamzPardakht.WebApi/ migrations add $msg
dotnet ef migrations script -i  --startup-project ../RamzPardakht.WebApi/
read


