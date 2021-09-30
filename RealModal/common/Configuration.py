from omegaconf import OmegaConf


def load_yaml(yaml_path):
    conf = OmegaConf.load(yaml_path)
    return conf


if __name__ == "__main__":
    print(load_yaml("../config/config.yaml"))