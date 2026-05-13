Feature: Parse namespaces into layers and slices

    Scenario Outline: Parse namespace
        Given rootnamespace is Nwwz.Product
        And namespace <namespace>
        When parsed
        Then root is <root>
        Then layer is <layer>
        Then slice is <slice>
        Then msut be analyzed is <TobeAnalyzed>

        Examples:
          | namespace                                | root         | layer  | slice    | TobeAnalyzed |
          | Nwwz.Product.Domain.FeatureA             | Nwwz.Product | Domain | FeatureA | true         |
          | Nwwz.Product.Infra.FeatureB              | Nwwz.Product | Infra  | FeatureB | true         |
          | Nwwz.Product.Domain                      | Nwwz.Product | Domain |          | true         |
          | Nwwz.Product                             | Nwwz.Product |        |          | false        |
          | Nwwz.Product.Domain.FeatureA.SubfeatureC | Nwwz.Product | Domain | FeatureA | true         |
          | System.Text.Json                         |              |        |          | false        |
          | Nwwz.System.Something                    |              |        |          | false        |