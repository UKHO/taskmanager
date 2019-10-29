package eventservice;

import org.junit.Test;

import static io.restassured.RestAssured.given;


public class Tests extends TestData {

    @Test
    public void eventServiceExampleTestWithSwaggerValidation() {

        given()
                .baseUri(EventServices_BaseUrl)
                .filter(EventServices_ValidationFilter)

        .when()
                .get("EventService/v1/Workflow/Event")
        

        .then()
                .assertThat()
                .statusCode(200);
    }
}