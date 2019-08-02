package dataservice;

import org.junit.Test;

import static io.restassured.RestAssured.given;


public class Tests extends TestData{

    @Test
    public void ExampleTest() {

        given()
                .when()
                .get(DataServices_BaseUrl)
                .then()
                .statusCode(200);

    }
}
