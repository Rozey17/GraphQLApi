module Model

type Person =
    { FirstName : string
      LastName : string }

let people =
    [ { FirstName = "Jane"
        LastName = "Milton" }
      { FirstName = "Travis"
        LastName = "Smith" } ]
