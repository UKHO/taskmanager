package dataservice;

import org.junit.Test;

import static io.restassured.RestAssured.given;


public class Tests extends TestData {

    @Test
    public void dataServiceExampleTestWithSwaggerValidation() {

        given()
                .baseUri(DataServices_BaseUrl)
                .filter(DataServices_ValidationFilter)

        .when()
                .get("/DataServices/v1/SourceDocument/Assessment/DocumentsForAssessment/HDB")

        .then()
                .assertThat()
                .statusCode(200);
    }
}