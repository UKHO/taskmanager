package dataservice;

import org.junit.Test;

import static io.restassured.RestAssured.given;

public class Tests {

    @Test
    public void ExampleTest() {

        String envTest = System.getenv("TM_DATASERVICES_URL");

        given()
                .when()
                .get(envTest)
                .then()
                .statusCode(200);
    }
}
