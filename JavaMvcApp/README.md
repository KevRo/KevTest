# JavaMvcApp

Java equivalent of the MyMvcApp project — a minimal Spring Boot MVC app with the same Home/Privacy pages, using Thymeleaf instead of Razor.

## Prerequisites

- **JDK 17+** — check with `java -version`
- **Maven** — check with `mvn -version` (or open the project in IntelliJ IDEA / Eclipse / VS Code with the Java extensions, which bundle their own Maven support and can run it without installing Maven separately)

## Run it

From this folder (the one containing `pom.xml`):

```
mvn spring-boot:run
```

Or build a jar and run it directly:

```
mvn clean package
java -jar target/javamvcapp-0.0.1-SNAPSHOT.jar
```

You'll see Spring Boot's startup log ending with something like:

```
Tomcat started on port(s): 8080 (http)
```

Open **http://localhost:8080** in your browser.

## Project layout

- `src/main/java/.../JavaMvcAppApplication.java` — app entry point
- `src/main/java/.../controller/HomeController.java` — Home and Privacy routes
- `src/main/resources/templates/` — Thymeleaf views (`index.html`, `privacy.html`, shared `fragments.html` for header/footer)
- `src/main/resources/static/` — static CSS/JS (Bootstrap pulled from CDN, no local copy needed)
- `src/main/resources/application.properties` — port and Thymeleaf config

## Note

This project was hand-written rather than generated via Spring Initializr, because the environment that built it had no internet access to Maven Central and no Maven installed, so it couldn't fetch dependencies or compile/run the project itself. The structure mirrors a standard Spring Boot Web + Thymeleaf project, so `mvn spring-boot:run` should work out of the box once you have JDK 17+ and Maven (or an IDE) available locally.
