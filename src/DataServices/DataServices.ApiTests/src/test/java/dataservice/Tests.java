package dataservice;

import org.junit.Test;

import static io.restassured.RestAssured.given;

public class Tests {

    @Test
    public void ExampleTest() {
        given()
                .when()
                .get("https://jsonplaceholder.typicode.com/posts")
                .then()
                .statusCode(200);
    }
}
