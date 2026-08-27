# StorePos Production Deployment Guide

## პირველი ინსტალაცია — StorePos.Api Windows Service-ად

ვივარაუდოთ, რომ API უკვე publish-ებულია:

```text
C:\StorePos\Api\
    StorePos.Api.exe
    appsettings.Production.json
    ...
```

გახსენით **Command Prompt / PowerShell — Run as Administrator**.

### 1. შექმენი Windows Service

```cmd
sc.exe create StorePosApi binPath= "C:\StorePos\Api\StorePos.Api.exe" start= auto DisplayName= "StorePos API"
```

### 2. ჩართე Delayed Automatic Start

```cmd
sc.exe config StorePosApi start= delayed-auto
```

### 3. Crash-ის შემდეგ ავტომატური Restart

```cmd
sc.exe failure StorePosApi reset= 86400 actions= restart/5000/restart/5000/restart/5000
```

```cmd
sc.exe failureflag StorePosApi 1
```

### 4. პირველად გაუშვი API

```cmd
sc.exe start StorePosApi
```

### 5. შეამოწმე სტატუსი

```cmd
sc.exe query StorePosApi
```

უნდა იყოს:

```text
STATE : 4 RUNNING
```

ასევე შეგიძლია გახსნა:

```text
services.msc
```

და შეამოწმო:

```text
StorePos API
Status: Running
Startup Type: Automatic (Delayed Start)
```

---

# შემდეგი Deployment / ახალი ვერსიის დანერგვა

Windows Service-ის თავიდან შექმნა ყოველ deployment-ზე **არ არის საჭირო**.

ყოველ ახალ ვერსიაზე:

```text
1. DB Backup (.bak)
2. Close StorePos.Desktop
3. Stop StorePosApi
4. Replace API publish files
5. Replace Desktop publish files, თუ შეიცვალა
6. Verify appsettings.Production.json
7. Start StorePosApi
8. Check service status
9. Check /health
10. Start StorePos.Desktop
```

## 1. Database Backup

Deployment-მდე აიღე SQL Server-ის სრული `.bak` backup.

ეს განსაკუთრებით მნიშვნელოვანია, თუ ახალ API ვერსიას ახალი EF Core migration მოაქვს.

## 2. დახურე StorePos.Desktop

თუ Desktop-ის ახალი ვერსიაც უნდა შეცვალო, Desktop აუცილებლად დახურული უნდა იყოს.

თუ მხოლოდ API იცვლება, Desktop-ის დახურვა მაინც სასურველია, რომ deployment-ის დროს API-ზე request-ები არ წავიდეს.

## 3. გააჩერე API

Administrator CMD/PowerShell:

```cmd
sc.exe stop StorePosApi
```

შეამოწმე:

```cmd
sc.exe query StorePosApi
```

უნდა იყოს:

```text
STATE : 1 STOPPED
```

## 4. ჩაანაცვლე API publish ფაილები

ახალი API publish ჩააკოპირე:

```text
C:\StorePos\Api\
```

ძველი ფაილების ნაცვლად.

დარწმუნდი, რომ:

```text
appsettings.Production.json
```

კვლავ სწორ Production SQL Server/Database-ზე მიუთითებს.

## 5. თუ Desktop-იც შეიცვალა

Desktop დახურული იყოს და ახალი publish ჩააკოპირე:

```text
C:\StorePos\Desktop\
```

## 6. გაუშვი ახალი API

```cmd
sc.exe start StorePosApi
```

## 7. შეამოწმე Service

```cmd
sc.exe query StorePosApi
```

უნდა იყოს:

```text
STATE : 4 RUNNING
```

## 8. შეამოწმე `/health`

მაგალითად:

```powershell
Invoke-RestMethod http://localhost:<PORT>/health
```

მოსალოდნელი პასუხი:

```json
{
  "status": "ready"
}
```

თუ pending EF Core migrations არსებობს, StorePos.Api startup-ისას ისინი შესრულდება `ApplyDatabaseMigrationsAsync()`-ის საშუალებით.

## 9. გაუშვი StorePos.Desktop

Desktop მხოლოდ მას შემდეგ გაუშვი, რაც:

```text
StorePosApi = RUNNING
/health = ready
```

---

# თუ მხოლოდ Desktop შეიცვალა

API-ს გაჩერება არ არის საჭირო, თუ API არ შეცვლილა.

```text
1. Close StorePos.Desktop
2. Replace C:\StorePos\Desktop\ files
3. Start StorePos.Desktop
```

# თუ მხოლოდ API შეიცვალა

```text
1. DB Backup
2. Preferably close StorePos.Desktop
3. sc.exe stop StorePosApi
4. Replace C:\StorePos\Api\ files
5. sc.exe start StorePosApi
6. Check /health
7. Reopen StorePos.Desktop
```

# მთავარი წესი

პირველი ინსტალაცია:

```text
sc.exe create ...
→ ერთხელ
```

შემდგომი deployment:

```text
STOP service
→ replace published files
→ START service
```

`Automatic (Delayed Start)` და crash recovery კონფიგურაცია შენარჩუნდება.
