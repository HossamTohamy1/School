# 🏫 School Management System

> A system that helps a school organize its classes, subjects and teachers — and builds the weekly timetable.

---

## 🧐 What is this, in plain words?

Every school has the same headache at the start of the year:
*Which teacher teaches which class? Which subject goes in which period? Who is free on Tuesday at 10 AM?*

Doing this on paper or Excel takes days and mistakes happen (the same teacher in two classrooms at once!).

This project is the **back-end (the "brain")** of a school system that keeps all of this organized:

- 🧑‍🏫 Register **teachers** and the times they are available.
- 📚 Define **subjects** and **classes** (e.g. *Grade 3 – A*).
- 🔗 Link which teacher teaches which subject to which class.
- 🗓️ Build and view the **weekly timetable** slot by slot.
- 👤 Manage **users** who can log in and work on the system.

## 👥 Who uses it?

| Who | What they can do |
|---|---|
| **School admin** | Add classes, subjects, teachers; build and edit the timetable |
| **Teachers** | See their own schedule and availability |

## 🔄 How it works (the simple story)

```
Add teachers + subjects + classes ➜ Set teacher availability ➜ Assign subjects to classes
        ➜ Fill the timetable slots ➜ Everyone sees their schedule
```

---

## 🛠️ For developers

**Tech stack:** ASP.NET Core Web API (.NET 9) · CQRS with MediatR · FluentValidation · AutoMapper · Entity Framework Core + SQL Server · Serilog · Swagger · Paged results

**Main entities:** `Class`, `Subject`, `Teacher`, `TeacherAvailability`, `ClassSubject`, `ClassTeacher`, `TimeTable`, `TimetableSlot`, `User`

**Run locally**

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download) and SQL Server.
2. In `School_managment/appsettings.json`, set your connection string (replace `CHANGE_ME`).
3. Run:
   ```bash
   cd School_managment
   dotnet ef database update
   dotnet run
   ```
4. Open `/swagger` to try the API.

---

## 🇪🇬 بالعربي

نظام لإدارة المدرسة: بيسجّل المدرسين والمواد والفصول، ويحدد مين بيدرّس إيه لأنهي فصل، ويعمل الجدول الأسبوعي من غير تعارض، يعني ما يبقاش نفس المدرس في فصلين في نفس الحصة. المشروع ده هو "العقل" اللي ورا النظام.
